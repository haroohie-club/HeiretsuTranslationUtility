using HaruhiHeiretsuLib.Graphics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace HaruhiHeiretsuEditor
{
    public class SgeWindow : GameWindow
    {
        public bool IsRunning { get; set; } = true;
        public SgeWindow(int width, int height, Sge model) : base(
            new() { UpdateFrequency = 60 },
            new() { Size = new(width, height), Title = $"SGE Render: {model.Name}", Flags = ContextFlags.ForwardCompatible }
            )
        {
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            base.OnUpdateFrame(args);
        }
    }
}
