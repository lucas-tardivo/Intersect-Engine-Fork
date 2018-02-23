﻿using System;
using Intersect.GameObjects;
using Intersect.Client.Classes.Localization;
using IntersectClientExtras.File_Management;
using IntersectClientExtras.GenericClasses;
using IntersectClientExtras.Graphics;
using IntersectClientExtras.Gwen.Control;
using IntersectClientExtras.Gwen.Control.EventArguments;
using IntersectClientExtras.Gwen.Input;
using IntersectClientExtras.Input;
using Intersect_Client.Classes.Core;
using Intersect_Client.Classes.General;
using Intersect_Client.Classes.Items;
using Intersect_Client.Classes.Networking;
using Intersect_Client.Classes.UI;
using Intersect_Client.Classes.UI.Game;

namespace Intersect.Client.Classes.UI.Game.Inventory
{
    public class InventoryItem
    {
        private int mCurrentItem = -2;
        private int mCurrentAmt = 0;
        private ItemDescWindow mDescWindow;

        //Drag/Drop References
        private InventoryWindow mInventoryWindow;

        private bool mIsEquipped;

        //Slot info
        private int mMySlot;

        //Dragging
        private bool mCanDrag;

        private long mClickTime;
        public ImagePanel Container;
        private Draggable mDragIcon;
        public ImagePanel EquipPanel;
        public bool IsDragging;

        //Mouse Event Variables
        private bool mMouseOver;

        private int mMouseX = -1;
        private int mMouseY = -1;
        public ImagePanel Pnl;
        private string mTexLoaded = "";

        public InventoryItem(InventoryWindow inventoryWindow, int index)
        {
            mInventoryWindow = inventoryWindow;
            mMySlot = index;
        }

        public void Setup()
        {
            Pnl = new ImagePanel(Container, "InventoryItemIcon");
            Pnl.HoverEnter += pnl_HoverEnter;
            Pnl.HoverLeave += pnl_HoverLeave;
            Pnl.RightClicked += pnl_RightClicked;
            Pnl.Clicked += pnl_Clicked;
            EquipPanel = new ImagePanel(Pnl, "InventoryItemEquippedIcon");
            EquipPanel.Texture = GameGraphics.Renderer.GetWhiteTexture();
        }

        void pnl_Clicked(Base sender, ClickedEventArgs arguments)
        {
            mClickTime = Globals.System.GetTimeMs() + 500;
        }

        void pnl_RightClicked(Base sender, ClickedEventArgs arguments)
        {
            if (Globals.GameShop != null)
            {
                Globals.Me.TrySellItem(mMySlot);
            }
            else if (Globals.InBank)
            {
                Globals.Me.TryDepositItem(mMySlot);
            }
            else if (Globals.InBag)
            {
                Globals.Me.TryStoreBagItem(mMySlot);
            }
            else if (Globals.InTrade)
            {
                Globals.Me.TryTradeItem(mMySlot);
            }
            else
            {
                Globals.Me.TryDropItem(mMySlot);
            }
        }

        void pnl_HoverLeave(Base sender, EventArgs arguments)
        {
            mMouseOver = false;
            mMouseX = -1;
            mMouseY = -1;
            if (mDescWindow != null)
            {
                mDescWindow.Dispose();
                mDescWindow = null;
            }
        }

        void pnl_HoverEnter(Base sender, EventArgs arguments)
        {
            mMouseOver = true;
            mCanDrag = true;
            if (Globals.InputManager.MouseButtonDown(GameInput.MouseButtons.Left))
            {
                mCanDrag = false;
                return;
            }
            if (mDescWindow != null)
            {
                mDescWindow.Dispose();
                mDescWindow = null;
            }
            if (Globals.GameShop == null)
            {
                mDescWindow = new ItemDescWindow(Globals.Me.Inventory[mMySlot].ItemNum,
                    Globals.Me.Inventory[mMySlot].ItemVal, mInventoryWindow.X - 255, mInventoryWindow.Y,
                    Globals.Me.Inventory[mMySlot].StatBoost);
            }
            else
            {
                ItemInstance invItem = Globals.Me.Inventory[mMySlot];
                ShopItem shopItem = null;
                for (int i = 0; i < Globals.GameShop.BuyingItems.Count; i++)
                {
                    var tmpShop = Globals.GameShop.BuyingItems[i];

                    if (invItem.ItemNum == tmpShop.ItemNum)
                    {
                        shopItem = tmpShop;
                        break;
                    }
                }

                if (Globals.GameShop.BuyingWhitelist && shopItem != null)
                {
                    var hoveredItem = ItemBase.Lookup.Get<ItemBase>(shopItem.CostItemNum);
                    if (hoveredItem != null)
                    {
                        mDescWindow = new ItemDescWindow(Globals.Me.Inventory[mMySlot].ItemNum,
                            Globals.Me.Inventory[mMySlot].ItemVal, mInventoryWindow.X - 220, mInventoryWindow.Y,
                            Globals.Me.Inventory[mMySlot].StatBoost, "",
                            Strings.Shop.sellsfor.ToString( shopItem.CostItemVal, hoveredItem.Name));
                    }
                }
                else if (shopItem == null)
                {
                    var hoveredItem = ItemBase.Lookup.Get<ItemBase>(invItem.ItemNum);
                    var costItem = ItemBase.Lookup.Get<ItemBase>(Globals.GameShop.DefaultCurrency);
                    if (hoveredItem != null && costItem != null)
                    {
                        mDescWindow = new ItemDescWindow(Globals.Me.Inventory[mMySlot].ItemNum,
                            Globals.Me.Inventory[mMySlot].ItemVal, mInventoryWindow.X - 220, mInventoryWindow.Y,
                            Globals.Me.Inventory[mMySlot].StatBoost, "",
                            Strings.Shop.sellsfor.ToString( hoveredItem.Price, costItem.Name));
                    }
                }
                else
                {
                    mDescWindow = new ItemDescWindow(invItem.ItemNum, invItem.ItemVal, mInventoryWindow.X - 255,
                        mInventoryWindow.Y, invItem.StatBoost, "", Strings.Shop.wontbuy);
                }
            }
        }

        public FloatRect RenderBounds()
        {
            FloatRect rect = new FloatRect()
            {
                X = Pnl.LocalPosToCanvas(new IntersectClientExtras.GenericClasses.Point(0, 0)).X,
                Y = Pnl.LocalPosToCanvas(new IntersectClientExtras.GenericClasses.Point(0, 0)).Y,
                Width = Pnl.Width,
                Height = Pnl.Height
            };
            return rect;
        }

        public void Update()
        {
            bool equipped = false;
            for (int i = 0; i < Options.EquipmentSlots.Count; i++)
            {
                if (Globals.Me.Equipment[i] == mMySlot)
                {
                    equipped = true;
                }
            }
            var item = ItemBase.Lookup.Get<ItemBase>(Globals.Me.Inventory[mMySlot].ItemNum);
            if (Globals.Me.Inventory[mMySlot].ItemNum != mCurrentItem || Globals.Me.Inventory[mMySlot].ItemVal != mCurrentAmt || equipped != mIsEquipped ||
                (item == null && mTexLoaded != "") || (item != null && mTexLoaded != item.Pic))
            {
                mCurrentItem = Globals.Me.Inventory[mMySlot].ItemNum;
                mCurrentAmt = Globals.Me.Inventory[mMySlot].ItemVal;
                mIsEquipped = equipped;
                EquipPanel.IsHidden = !mIsEquipped;
                if (item != null)
                {
                    GameTexture itemTex = Globals.ContentManager.GetTexture(GameContentManager.TextureType.Item,
                        item.Pic);
                    if (itemTex != null)
                    {
                        Pnl.Texture = itemTex;
                    }
                    else
                    {
                        if (Pnl.Texture != null)
                        {
                            Pnl.Texture = null;
                        }
                    }
                    mTexLoaded = item.Pic;
                }
                else
                {
                    if (Pnl.Texture != null)
                    {
                        Pnl.Texture = null;
                    }
                    mTexLoaded = "";
                }
                if (mDescWindow != null)
                {
                    mDescWindow.Dispose();
                    mDescWindow = null;
                    pnl_HoverEnter(null, null);
                }
            }
            if (!IsDragging)
            {
                if (mMouseOver)
                {
                    if (!Globals.InputManager.MouseButtonDown(GameInput.MouseButtons.Left))
                    {
                        mCanDrag = true;
                        mMouseX = -1;
                        mMouseY = -1;
                        if (Globals.System.GetTimeMs() < mClickTime)
                        {
                            Globals.Me.TryUseItem(mMySlot);
                            mClickTime = 0;
                        }
                    }
                    else
                    {
                        if (mCanDrag)
                        {
                            if (mMouseX == -1 || mMouseY == -1)
                            {
                                mMouseX = InputHandler.MousePosition.X -
                                         Pnl.LocalPosToCanvas(new IntersectClientExtras.GenericClasses.Point(0, 0)).X;
                                mMouseY = InputHandler.MousePosition.Y -
                                         Pnl.LocalPosToCanvas(new IntersectClientExtras.GenericClasses.Point(0, 0)).Y;
                            }
                            else
                            {
                                int xdiff = mMouseX -
                                            (InputHandler.MousePosition.X -
                                             Pnl.LocalPosToCanvas(new IntersectClientExtras.GenericClasses.Point(0, 0))
                                                 .X);
                                int ydiff = mMouseY -
                                            (InputHandler.MousePosition.Y -
                                             Pnl.LocalPosToCanvas(new IntersectClientExtras.GenericClasses.Point(0, 0))
                                                 .Y);
                                if (Math.Sqrt(Math.Pow(xdiff, 2) + Math.Pow(ydiff, 2)) > 5)
                                {
                                    IsDragging = true;
                                    mDragIcon = new Draggable(
                                        Pnl.LocalPosToCanvas(new IntersectClientExtras.GenericClasses.Point(0, 0)).X +
                                        mMouseX,
                                        Pnl.LocalPosToCanvas(new IntersectClientExtras.GenericClasses.Point(0, 0)).X +
                                        mMouseY, Pnl.Texture);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (mDragIcon.Update())
                {
                    //Drug the item and now we stopped
                    IsDragging = false;
                    FloatRect dragRect = new FloatRect(
                        mDragIcon.X - (Container.Padding.Left + Container.Padding.Right) / 2,
                        mDragIcon.Y - (Container.Padding.Top + Container.Padding.Bottom) / 2,
                        (Container.Padding.Left + Container.Padding.Right) / 2 + Pnl.Width,
                        (Container.Padding.Top + Container.Padding.Bottom) / 2 + Pnl.Height);

                    float bestIntersect = 0;
                    int bestIntersectIndex = -1;
                    //So we picked up an item and then dropped it. Lets see where we dropped it to.
                    //Check inventory first.
                    if (mInventoryWindow.RenderBounds().IntersectsWith(dragRect))
                    {
                        for (int i = 0; i < Options.MaxInvItems; i++)
                        {
                            if (mInventoryWindow.Items[i].RenderBounds().IntersectsWith(dragRect))
                            {
                                if (FloatRect.Intersect(mInventoryWindow.Items[i].RenderBounds(), dragRect).Width *
                                    FloatRect.Intersect(mInventoryWindow.Items[i].RenderBounds(), dragRect).Height >
                                    bestIntersect)
                                {
                                    bestIntersect =
                                        FloatRect.Intersect(mInventoryWindow.Items[i].RenderBounds(), dragRect).Width *
                                        FloatRect.Intersect(mInventoryWindow.Items[i].RenderBounds(), dragRect).Height;
                                    bestIntersectIndex = i;
                                }
                            }
                        }
                        if (bestIntersectIndex > -1)
                        {
                            if (mMySlot != bestIntersectIndex)
                            {
                                //Try to swap....
                                PacketSender.SendSwapItems(bestIntersectIndex, mMySlot);
                                Globals.Me.SwapItems(bestIntersectIndex, mMySlot);
                            }
                        }
                    }
                    else if (Gui.GameUi.Hotbar.RenderBounds().IntersectsWith(dragRect))
                    {
                        for (int i = 0; i < Options.MaxHotbar; i++)
                        {
                            if (Gui.GameUi.Hotbar.Items[i].RenderBounds().IntersectsWith(dragRect))
                            {
                                if (FloatRect.Intersect(Gui.GameUi.Hotbar.Items[i].RenderBounds(), dragRect).Width *
                                    FloatRect.Intersect(Gui.GameUi.Hotbar.Items[i].RenderBounds(), dragRect).Height >
                                    bestIntersect)
                                {
                                    bestIntersect =
                                        FloatRect.Intersect(Gui.GameUi.Hotbar.Items[i].RenderBounds(), dragRect).Width *
                                        FloatRect.Intersect(Gui.GameUi.Hotbar.Items[i].RenderBounds(), dragRect).Height;
                                    bestIntersectIndex = i;
                                }
                            }
                        }
                        if (bestIntersectIndex > -1)
                        {
                            Globals.Me.AddToHotbar(bestIntersectIndex, 0, mMySlot);
                        }
                    }

                    mDragIcon.Dispose();
                }
            }
        }
    }
}