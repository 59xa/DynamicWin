using DynamicWin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets.Small
{
    class RegisterSmallVisualiserWidget : IRegisterableWidget
    {
        public bool IsSmallWidget => true;
        public string WidgetName => "Audio Visualiser";

        public WidgetBase CreateWidgetInstance(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter)
        {
            return new SmallVisualiserWidget(parent, position, alignment);
        }
    }

    public class SmallVisualiserWidget : SmallWidgetBase
    {
        AudioVisualiser audioVisualiser;

        public SmallVisualiserWidget(UIObject? parent, Vec2 position, UIAlignment alignment = UIAlignment.TopCenter) : base(parent, position, alignment)
        {
            audioVisualiser = new AudioVisualiser(this, new Vec2(0, 0), new Vec2(GetWidgetSize().X, GetWidgetSize().Y - 2), UIAlignment.Center);
            audioVisualiser.EnableColourTransition = false;
            audioVisualiser.EnableDotWhenLow = true;
            AddLocalObject(audioVisualiser);
        }

        protected override float GetWidgetWidth()
        {
            return base.GetWidgetWidth() - 10;
        }
    }
}
