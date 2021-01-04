using System;
using System.Drawing;
using System.Windows.Forms;

namespace CardReaderOne.Typer {

    internal class SystemTray : ApplicationContext {
        private readonly NotifyIcon trayIcon;

        public SystemTray() {
            trayIcon = new NotifyIcon {
                Icon = new Icon(SystemIcons.Application, 40, 40),
                ContextMenu = new ContextMenu(new MenuItem[] {
                            new MenuItem("Exit", ExitApplication),
                        }),
                Text = "CardReaderOne - Typer",
                Visible = true
            };
        }

        public void ExitApplication(object sender, EventArgs args) {
            trayIcon.Visible = false;

            Application.Exit();
        }
    }
}