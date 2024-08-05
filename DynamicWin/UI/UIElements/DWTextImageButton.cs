﻿using DynamicWin.Utils;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.UIElements
{
    internal class DWTextImageButton : DWButton
    {
        DWText text;

        public DWText Text { get { return text; } set => text = value; }

        public float normalTextSize = 10;
        public float textSizeSmoothSpeed = 15f;

        DWImage image;
        public float imageScale = 0.85f;

        public DWImage Image { get { return image; } private set => image = value; }

        public DWTextImageButton(UIObject? parent, SKBitmap sprite, string buttonText, Vec2 position, Vec2 size, Action clickCallback, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, size, clickCallback, alignment)
        {
            text = new DWText(this, buttonText, new Vec2(-7.5f, 0), UIAlignment.MiddleRight);
            text.Anchor.X = 0f;
            AddLocalObject(text);

            image = new DWImage(this, sprite, new Vec2(15, 0), Vec2.one * size.Y * imageScale, UIAlignment.MiddleLeft);
            text.Anchor.X = 1f;
            AddLocalObject(image);

            Text.textSize = normalTextSize;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            float currentTextSize = normalTextSize;

            Image.Size = Vec2.one * Size.Y * imageScale;

            if (IsHovering && !IsMouseDown)
                currentTextSize *= hoverScaleMulti.Magnitude;
            else if (IsMouseDown)
                currentTextSize *= clickScaleMulti.Magnitude;
            else if (!IsHovering && !IsMouseDown)
                currentTextSize *= normalScaleMulti.Magnitude;
            else
                currentTextSize *= normalScaleMulti.Magnitude;

            Text.textSize = Mathf.Lerp(Text.textSize, currentTextSize, textSizeSmoothSpeed * deltaTime);
        }
    }
}