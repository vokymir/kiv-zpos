using ImGuiNET;
using System.Numerics;

namespace Visualizer.UI
{
    public static class OxocarbonTheme
    {
        // IBM Carbon Design System — Oxocarbon dark palette
        private static Vector4 Hex(uint rgb, float a = 1f)
        {
            float r = ((rgb >> 16) & 0xFF) / 255f;
            float g = ((rgb >> 8)  & 0xFF) / 255f;
            float b = ( rgb        & 0xFF) / 255f;
            return new Vector4(r, g, b, a);
        }

        // Base surfaces
        public static readonly Vector4 Base00  = Hex(0x161616); // deepest bg
        public static readonly Vector4 Base01  = Hex(0x1e1e1e); // alt row
        public static readonly Vector4 Base02  = Hex(0x262626); // surface
        public static readonly Vector4 Base03  = Hex(0x393939); // raised surface
        public static readonly Vector4 Base04  = Hex(0x525252); // border / subtle
        public static readonly Vector4 Base05  = Hex(0x6f6f6f); // disabled text
        public static readonly Vector4 Base06  = Hex(0xc1c7cd); // secondary text
        public static readonly Vector4 Base07  = Hex(0xf2f4f8); // primary text

        // Accent — Carbon blue
        public static readonly Vector4 Blue    = Hex(0x78a9ff);
        public static readonly Vector4 BlueDim = Hex(0x78a9ff, 0.30f);
        public static readonly Vector4 BlueHi  = Hex(0xa6c8ff);

        public static void Apply()
        {
            ImGuiStylePtr s = ImGui.GetStyle();

            // Geometry
            s.WindowPadding        = new Vector2(10, 8);
            s.FramePadding         = new Vector2(6, 4);
            s.ItemSpacing          = new Vector2(6, 4);
            s.ItemInnerSpacing     = new Vector2(4, 4);
            s.IndentSpacing        = 18f;
            s.ScrollbarSize        = 10f;
            s.GrabMinSize          = 10f;
            s.WindowBorderSize     = 1f;
            s.ChildBorderSize      = 1f;
            s.PopupBorderSize      = 1f;
            s.FrameBorderSize      = 0f;
            s.TabBorderSize        = 0f;
            s.WindowRounding       = 4f;
            s.ChildRounding        = 4f;
            s.FrameRounding        = 3f;
            s.PopupRounding        = 4f;
            s.ScrollbarRounding    = 4f;
            s.GrabRounding         = 3f;
            s.TabRounding          = 3f;

            var c = s.Colors;

            c[(int)ImGuiCol.Text]                  = Base07;
            c[(int)ImGuiCol.TextDisabled]           = Base05;
            c[(int)ImGuiCol.WindowBg]               = Base00;
            c[(int)ImGuiCol.ChildBg]                = Base00;
            c[(int)ImGuiCol.PopupBg]                = Base02;
            c[(int)ImGuiCol.Border]                 = Base04;
            c[(int)ImGuiCol.BorderShadow]           = Hex(0x000000, 0f);
            c[(int)ImGuiCol.FrameBg]                = Base02;
            c[(int)ImGuiCol.FrameBgHovered]         = Base03;
            c[(int)ImGuiCol.FrameBgActive]          = Hex(0x4c4c4c);
            c[(int)ImGuiCol.TitleBg]                = Base00;
            c[(int)ImGuiCol.TitleBgActive]          = Base02;
            c[(int)ImGuiCol.TitleBgCollapsed]       = Base00;
            c[(int)ImGuiCol.MenuBarBg]              = Base00;
            c[(int)ImGuiCol.ScrollbarBg]            = Base00;
            c[(int)ImGuiCol.ScrollbarGrab]          = Base04;
            c[(int)ImGuiCol.ScrollbarGrabHovered]   = Base05;
            c[(int)ImGuiCol.ScrollbarGrabActive]    = Base06;
            c[(int)ImGuiCol.CheckMark]              = Blue;
            c[(int)ImGuiCol.SliderGrab]             = Blue;
            c[(int)ImGuiCol.SliderGrabActive]       = BlueHi;
            c[(int)ImGuiCol.Button]                 = Base02;
            c[(int)ImGuiCol.ButtonHovered]          = Base03;
            c[(int)ImGuiCol.ButtonActive]           = Hex(0x4c4c4c);
            c[(int)ImGuiCol.Header]                 = Base02;
            c[(int)ImGuiCol.HeaderHovered]          = Base03;
            c[(int)ImGuiCol.HeaderActive]           = Hex(0x4c4c4c);
            c[(int)ImGuiCol.Separator]              = Base04;
            c[(int)ImGuiCol.SeparatorHovered]       = Blue;
            c[(int)ImGuiCol.SeparatorActive]        = BlueHi;
            c[(int)ImGuiCol.ResizeGrip]             = Base04;
            c[(int)ImGuiCol.ResizeGripHovered]      = Blue;
            c[(int)ImGuiCol.ResizeGripActive]       = BlueHi;
            c[(int)ImGuiCol.Tab]                    = Base02;
            c[(int)ImGuiCol.TabHovered]             = Base03;
            c[(int)ImGuiCol.TabSelected]            = Base03;
            c[(int)ImGuiCol.TabSelectedOverline]    = Blue;
            c[(int)ImGuiCol.TabDimmed]              = Base00;
            c[(int)ImGuiCol.TabDimmedSelected]      = Base02;
            c[(int)ImGuiCol.TabDimmedSelectedOverline] = Base04;
            c[(int)ImGuiCol.DockingPreview]         = BlueDim;
            c[(int)ImGuiCol.DockingEmptyBg]         = Base00;
            c[(int)ImGuiCol.PlotLines]              = Blue;
            c[(int)ImGuiCol.PlotLinesHovered]       = BlueHi;
            c[(int)ImGuiCol.PlotHistogram]          = Blue;
            c[(int)ImGuiCol.PlotHistogramHovered]   = BlueHi;
            c[(int)ImGuiCol.TableHeaderBg]          = Base02;
            c[(int)ImGuiCol.TableBorderStrong]      = Base04;
            c[(int)ImGuiCol.TableBorderLight]       = Base03;
            c[(int)ImGuiCol.TableRowBg]             = Hex(0x000000, 0f);
            c[(int)ImGuiCol.TableRowBgAlt]          = Hex(0x1e1e1e, 0.5f);
            c[(int)ImGuiCol.TextSelectedBg]         = BlueDim;
            c[(int)ImGuiCol.DragDropTarget]         = Blue;
            c[(int)ImGuiCol.NavCursor]              = Blue;
            c[(int)ImGuiCol.NavWindowingHighlight]  = Hex(0xffffff, 0.70f);
            c[(int)ImGuiCol.NavWindowingDimBg]      = Hex(0x000000, 0.50f);
            c[(int)ImGuiCol.ModalWindowDimBg]       = Hex(0x000000, 0.60f);
        }
    }
}
