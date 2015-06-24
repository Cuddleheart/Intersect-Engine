﻿using Gwen;
using Gwen.Control;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Intersect_Client.Classes.UI.Game
{
    public class InventoryWindow : IGUIElement
    {
        //Controls
        private WindowControl _inventoryWindow;
        private ScrollControl _itemContainer;

        //Location
        public int X;
        public int Y;

        //Item List
        public List<InventoryItem> Items = new List<InventoryItem>();
        private List<Label> _values = new List<Label>();

        //Init
        public InventoryWindow(Canvas _gameCanvas)
        {
            _inventoryWindow = new WindowControl(_gameCanvas, "Inventory");
            _inventoryWindow.SetSize(200, 300);
            _inventoryWindow.SetPosition(Graphics.ScreenWidth - 210, Graphics.ScreenHeight - 500);
            _inventoryWindow.DisableResizing();
            _inventoryWindow.Margin = Margin.Zero;
            _inventoryWindow.Padding = Padding.Zero;
            _inventoryWindow.IsHidden = true;

            _itemContainer = new ScrollControl(_inventoryWindow);
            _itemContainer.SetPosition(0, 0);
            _itemContainer.SetSize(_inventoryWindow.Width, _inventoryWindow.Height - 24);
            _itemContainer.EnableScroll(false, true);
            InitItemContainer();
        }

        //Methods
        public void Update()
        {
            if (_inventoryWindow.IsHidden == true) { return; }
            X = _inventoryWindow.X;
            Y = _inventoryWindow.Y;
            for (int i = 0; i < Constants.MaxInvItems; i++)
            {
                if (Globals.Me.Inventory[i].ItemNum > -1)
                {
                    Items[i].pnl.IsHidden = false;

                    if (Globals.GameItems[Globals.Me.Inventory[i].ItemNum].Type == (int)Enums.ItemTypes.Consumable || //Allow Stacking on Currency, Consumable, Spell, and item types of none.
                        Globals.GameItems[Globals.Me.Inventory[i].ItemNum].Type == (int)Enums.ItemTypes.Currency ||
                        Globals.GameItems[Globals.Me.Inventory[i].ItemNum].Type == (int)Enums.ItemTypes.None ||
                        Globals.GameItems[Globals.Me.Inventory[i].ItemNum].Type == (int)Enums.ItemTypes.Spell)
                    {
                        _values[i].IsHidden = false;
                        _values[i].Text = Globals.Me.Inventory[i].ItemVal.ToString();
                    }
                    else
                    {
                        _values[i].IsHidden = true;
                    }

                    if (Items[i].IsDragging)
                    {
                        Items[i].pnl.IsHidden = true;
                        _values[i].IsHidden = true;
                    }
                    Items[i].Update();
                }
                else
                {
                    Items[i].pnl.IsHidden = true;
                    _values[i].IsHidden = true;
                }
            }
        }
        private void InitItemContainer()
        {

            for (int i = 0; i < Constants.MaxInvItems; i++)
            {
                Items.Add(new InventoryItem(this, i));
                Items[i].pnl = new ImagePanel(_itemContainer);
                Items[i].pnl.SetSize(32, 32);
                Items[i].pnl.SetPosition((i % (_itemContainer.Width / (32 + Constants.ItemXPadding))) * (32 + Constants.ItemXPadding) + Constants.ItemXPadding, (i / (_itemContainer.Width / (32 + Constants.ItemXPadding))) * (32 + Constants.ItemYPadding) + Constants.ItemYPadding);
                Items[i].pnl.Clicked += InventoryWindow_Clicked;
                Items[i].pnl.IsHidden = true;
                Items[i].Setup();

                _values.Add(new Label(_itemContainer));
                _values[i].Text = "";
                _values[i].SetPosition((i % (_itemContainer.Width / (32 + Constants.ItemXPadding))) * (32 + Constants.ItemXPadding) + Constants.ItemXPadding, (i / (_itemContainer.Width / (32 + Constants.ItemXPadding))) * (32 + Constants.ItemYPadding) + Constants.ItemYPadding + 24);
                _values[i].TextColor = System.Drawing.Color.Black;
                _values[i].MakeColorDark();
                _values[i].IsHidden = true;
            }
        }

        void InventoryWindow_Clicked(Base sender, ClickedEventArgs arguments)
        {

        }
        public void Show()
        {
            _inventoryWindow.IsHidden = false;
        }
        public bool IsVisible()
        {
            return !_inventoryWindow.IsHidden;
        }
        public void Hide()
        {
            _inventoryWindow.IsHidden = true;
        }
        public System.Drawing.Rectangle RenderBounds()
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle();
            rect.X = _inventoryWindow.LocalPosToCanvas(new System.Drawing.Point(0, 0)).X - Constants.ItemXPadding / 2;
            rect.Y = _inventoryWindow.LocalPosToCanvas(new System.Drawing.Point(0, 0)).Y - Constants.ItemYPadding / 2;
            rect.Width = _inventoryWindow.Width + Constants.ItemXPadding;
            rect.Height = _inventoryWindow.Height + Constants.ItemYPadding;
            return rect;
        }
    }

    public class InventoryItem
    {
        public ImagePanel pnl;
        private ItemDescWindow _descWindow;

        //Mouse Event Variables
        private bool MouseOver = false;
        private int MouseX = -1;
        private int MouseY = -1;
        private long ClickTime = 0;

        //Dragging
        private bool CanDrag = false;
        private Draggable dragIcon;
        public bool IsDragging;

        //Slot info
        private int _mySlot;
        private bool _isEquipped;
        private int _currentItem = -2;

        //Drag/Drop References
        private InventoryWindow _inventoryWindow;
 

        public InventoryItem(InventoryWindow inventoryWindow, int index)
        {
            _inventoryWindow = inventoryWindow;
            _mySlot = index;
        }

        public void Setup()
        {
            pnl.HoverEnter += pnl_HoverEnter;
            pnl.HoverLeave += pnl_HoverLeave;
            pnl.RightClicked += pnl_RightClicked;
            pnl.Clicked += pnl_Clicked;
        }

        void pnl_Clicked(Base sender, ClickedEventArgs arguments)
        {
            ClickTime = Environment.TickCount + 500;
        }

        void pnl_RightClicked(Base sender, ClickedEventArgs arguments)
        {
            Globals.Me.TryDropItem(_mySlot);
        }

        void pnl_HoverLeave(Base sender, EventArgs arguments)
        {
            MouseOver = false;
            MouseX = -1;
            MouseY = -1;
            if (_descWindow != null) { _descWindow.Dispose(); _descWindow = null; }
        }

        void pnl_HoverEnter(Base sender, EventArgs arguments)
        {
            MouseOver = true;
            CanDrag = true;
            if (Mouse.IsButtonPressed(Mouse.Button.Left)) { CanDrag = false; return; }
            if (_descWindow != null) { _descWindow.Dispose(); _descWindow = null; }
            _descWindow = new ItemDescWindow(Globals.Me.Inventory[_mySlot].ItemNum, Globals.Me.Inventory[_mySlot].ItemVal, _inventoryWindow.X - 220, _inventoryWindow.Y, Globals.Me.Inventory[_mySlot].StatBoost);
        }

        public System.Drawing.Rectangle RenderBounds()
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle();
            rect.X = pnl.LocalPosToCanvas(new System.Drawing.Point(0, 0)).X;
            rect.Y = pnl.LocalPosToCanvas(new System.Drawing.Point(0, 0)).Y;
            rect.Width = pnl.Width;
            rect.Height = pnl.Height;
            return rect;
        }

        public void Update()
        {
            bool equipped = false;
            for (int i = 0; i < Enums.EquipmentSlots.Count; i++)
            {
                if (Globals.Me.Equipment[i] == _mySlot)
                {
                    equipped = true;
                }
            }
            if (Globals.Me.Inventory[_mySlot].ItemNum != _currentItem || equipped != _isEquipped)
            {
                _currentItem = Globals.Me.Inventory[_mySlot].ItemNum;
                _isEquipped = equipped;
                pnl.Texture = Gui.BitmapToGwenTexture(Gui.CreateImageTexBitmap(_currentItem,0,0,32,32,_isEquipped,null));
            }
            if (!IsDragging)
            {
                if (MouseOver)
                {
                    if (!Mouse.IsButtonPressed(Mouse.Button.Left))
                    {
                        CanDrag = true;
                        MouseX = -1;
                        MouseY = -1;
                        if (Environment.TickCount < ClickTime)
                        {
                            Globals.Me.TryUseItem(_mySlot);
                            ClickTime = 0;
                        }
                    }
                    else
                    {
                        if (CanDrag)
                        {
                            if (MouseX == -1 || MouseY == -1)
                            {
                                MouseX = Gwen.Input.InputHandler.MousePosition.X - pnl.LocalPosToCanvas(new System.Drawing.Point(0, 0)).X;
                                MouseY = Gwen.Input.InputHandler.MousePosition.Y - pnl.LocalPosToCanvas(new System.Drawing.Point(0, 0)).Y;

                            }
                            else
                            {
                                int xdiff = MouseX - (Gwen.Input.InputHandler.MousePosition.X - pnl.LocalPosToCanvas(new System.Drawing.Point(0, 0)).X);
                                int ydiff = MouseY - (Gwen.Input.InputHandler.MousePosition.Y - pnl.LocalPosToCanvas(new System.Drawing.Point(0, 0)).Y);
                                if (Math.Sqrt(Math.Pow(xdiff, 2) + Math.Pow(ydiff, 2)) > 5)
                                {
                                    IsDragging = true;
                                    dragIcon = new Draggable(pnl.LocalPosToCanvas(new System.Drawing.Point(0, 0)).X + MouseX, pnl.LocalPosToCanvas(new System.Drawing.Point(0, 0)).X + MouseY, pnl.Texture);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (dragIcon.Update())
                {
                    //Drug the item and now we stopped
                    IsDragging = false;
                    System.Drawing.Rectangle dragRect = new System.Drawing.Rectangle(dragIcon.x - Constants.ItemXPadding / 2, dragIcon.y - Constants.ItemYPadding / 2, Constants.ItemXPadding/2 + 32, Constants.ItemYPadding / 2 + 32);

                    int bestIntersect = 0;
                    int bestIntersectIndex = -1;
                    //So we picked up an item and then dropped it. Lets see where we dropped it to.
                    //Check inventory first.
                    if (_inventoryWindow.RenderBounds().IntersectsWith(dragRect))
                    {
                        for (int i = 0; i < Constants.MaxInvItems; i++)
                        {
                            if (_inventoryWindow.Items[i].RenderBounds().IntersectsWith(dragRect))
                            {
                                if (System.Drawing.Rectangle.Intersect(_inventoryWindow.Items[i].RenderBounds(), dragRect).Width * System.Drawing.Rectangle.Intersect(_inventoryWindow.Items[i].RenderBounds(), dragRect).Height > bestIntersect)
                                {
                                    bestIntersect = System.Drawing.Rectangle.Intersect(_inventoryWindow.Items[i].RenderBounds(), dragRect).Width * System.Drawing.Rectangle.Intersect(_inventoryWindow.Items[i].RenderBounds(), dragRect).Height;
                                    bestIntersectIndex = i;
                                }
                            }
                        }
                        if (bestIntersectIndex > -1)
                        {
                            if (_mySlot != bestIntersectIndex)
                            {
                                //Try to swap....
                                PacketSender.SendSwapItems(bestIntersectIndex, _mySlot);
                                Globals.Me.SwapItems(bestIntersectIndex, _mySlot);
                            }
                        }
                    }
                    else if (Gui._GameGui.Hotbar.RenderBounds().IntersectsWith(dragRect))
                    {
                        for (int i = 0; i < Constants.MaxHotbar; i++)
                        {
                            if (Gui._GameGui.Hotbar.Items[i].RenderBounds().IntersectsWith(dragRect))
                            {
                                if (System.Drawing.Rectangle.Intersect(Gui._GameGui.Hotbar.Items[i].RenderBounds(), dragRect).Width * System.Drawing.Rectangle.Intersect(Gui._GameGui.Hotbar.Items[i].RenderBounds(), dragRect).Height > bestIntersect)
                                {
                                    bestIntersect = System.Drawing.Rectangle.Intersect(Gui._GameGui.Hotbar.Items[i].RenderBounds(), dragRect).Width * System.Drawing.Rectangle.Intersect(Gui._GameGui.Hotbar.Items[i].RenderBounds(), dragRect).Height;
                                    bestIntersectIndex = i;
                                }
                            }
                        }
                        if (bestIntersectIndex > -1)
                        {
                            Globals.Me.AddToHotbar(bestIntersectIndex, 0, _mySlot);
                        }
                    }

                    dragIcon.Dispose();
                }
            }
        }
    }
}
