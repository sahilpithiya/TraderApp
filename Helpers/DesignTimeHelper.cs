using ClientDesktop.HelperClass;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TraderApps.Config;
using WeifenLuo.WinFormsUI.Docking;

namespace TraderApps
{
    public class DesignTimeHelper
    {
        #region Fields And Properties
        private readonly DockPanel dockPanel;

        private const string LayoutFileName = "layout.xml";
        private string LayoutFilePath => Path.Combine(AppConfig.dataFolder, LayoutFileName);
        #endregion

        #region Constructor
        public DesignTimeHelper(DockPanel dockPanel)
        {
            this.dockPanel = dockPanel;
            this.dockPanel.DockLeftPortion = 450;
        }
        #endregion

        #region Layout Load And Save
        public void LoadLayout(DeserializeDockContent deserializeCallback)
        {
            try
            {
                if (File.Exists(LayoutFilePath))
                    dockPanel.LoadFromXml(LayoutFilePath, deserializeCallback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load layout: {ex.Message}",
                    "Layout Error");
            }
        }

        public void SaveLayout()
        {
            try
            {
                var dir = Path.GetDirectoryName(LayoutFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                dockPanel.SaveAsXml(LayoutFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save layout: {ex.Message}",
                    "Layout Error");
            }
        }

        public void ResetLayout()
        {
            try
            {
                if (File.Exists(LayoutFilePath))
                    File.Delete(LayoutFilePath);
            }
            catch { }
        }
        #endregion

        #region Inner DockContent Class
        public class DynamicDockContent : DockContent
        {
            private readonly string persistString;

            public DynamicDockContent(string persistString, Control content = null)
            {
                this.persistString = persistString ?? throw new ArgumentNullException(nameof(persistString));
                this.Text = persistString;

                if (content != null)
                {
                    content.BackColor = Color.White;
                    content.Dock = DockStyle.Fill;
                    Controls.Add(content);
                }
            }

            protected override string GetPersistString() => persistString;
        }
        #endregion

        #region Dynamic Color Theme Class
        public class DynamicColorTheme : VS2015LightTheme
        {
            public DynamicColorTheme(Color baseColor)
            {
                ApplyColorWhite(baseColor);
                this.Extender.DockPaneCaptionFactory = new NoPinCaptionFactoryWrapper(this.Extender.DockPaneCaptionFactory);
            }

            public void ApplyColorWhite(Color baseColor)
            {
                var c = ColorPalette;
                c.MainWindowActive.Background = ThemeManager.White;
                c.AutoHideStripDefault.Background = ThemeManager.SelectionBackColor;
                c.AutoHideStripDefault.Text = ThemeManager.White;
                c.AutoHideStripDefault.Border = ThemeManager.White;
                c.AutoHideStripHovered.Text = Color.White;
                c.AutoHideStripHovered.Background = ThemeManager.SelectionBackColor;
                c.AutoHideStripHovered.Border = ThemeManager.White;
            }

            public void ApplyColor(Color baseColor)
            {
                var c = ColorPalette;
                Color light = ControlPaint.Light(baseColor, 0.3f);
                Color lighter = ControlPaint.Light(baseColor, 0.6f);
                Color dark = ControlPaint.Dark(baseColor, 0.3f);
                Color darker = ControlPaint.Dark(baseColor, 0.6f);

                c.MainWindowActive.Background = darker;
                c.AutoHideStripDefault.Background = dark;
                c.TabSelectedActive.Background = baseColor;
                c.TabSelectedActive.Text = ThemeManager.White;
                c.TabSelectedInactive.Background = dark;
                c.TabSelectedInactive.Text = Color.Gainsboro;
                c.TabUnselected.Background = darker;
                c.TabUnselected.Text = Color.LightGray;
                c.TabUnselectedHovered.Background = light;
                c.TabUnselectedHovered.Text = ThemeManager.White;
                c.ToolWindowCaptionActive.Background = baseColor;
                c.ToolWindowCaptionActive.Text = ThemeManager.White;
                c.ToolWindowCaptionInactive.Background = dark;
                c.ToolWindowCaptionInactive.Text = Color.Silver;
                c.ToolWindowTabSelectedActive.Background = light;
                c.ToolWindowTabSelectedActive.Text = ThemeManager.White;
                c.ToolWindowTabUnselected.Background = darker;
                c.ToolWindowTabUnselected.Text = ThemeManager.Gray;
                c.DockTarget.Background = baseColor;
                c.CommandBarMenuDefault.Background = dark;
                c.CommandBarMenuDefault.Text = Color.WhiteSmoke;
            }
        }
        #endregion

        #region Factory Wrapper
        private class NoPinCaptionFactoryWrapper : DockPanelExtender.IDockPaneCaptionFactory
        {
            private readonly DockPanelExtender.IDockPaneCaptionFactory _original;

            public NoPinCaptionFactoryWrapper(DockPanelExtender.IDockPaneCaptionFactory original)
            {
                _original = original;
            }

            public DockPaneCaptionBase CreateDockPaneCaption(DockPane pane)
            {
                var caption = _original.CreateDockPaneCaption(pane);

                if (caption is Control control)
                {
                    control.Layout += (s, e) => HidePinButton(control);
                }

                return caption;
            }

            private void HidePinButton(Control captionControl)
            {
                var visibleButtons = captionControl.Controls.Cast<Control>()
                    .Where(c => c.Visible)
                    .OrderByDescending(c => c.Right)
                    .ToList();

                if (visibleButtons.Count > 1)
                {
                    for (int i = 1; i < visibleButtons.Count; i++)
                    {
                        visibleButtons[i].Visible = false;
                    }
                }
            }
        }
        #endregion
    }
}