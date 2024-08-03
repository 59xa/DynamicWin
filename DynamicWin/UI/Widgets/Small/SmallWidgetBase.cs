﻿using DynamicWin.Main;
using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Small
{
    internal class SmallWidgetBase : WidgetBase
    {
        public SmallWidgetBase(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            roundRadius = 5f;
        }

        protected override float GetWidgetHeight() { return 15; }
        protected override float GetWidgetWidth() { return 35; }
    }
}