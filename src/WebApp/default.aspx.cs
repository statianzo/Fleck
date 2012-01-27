using System;
using System.Web.UI;

namespace WebApp
{
    public partial class _default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void timer1_onTick(object sender, EventArgs e)
        {
            string logText = Convert.ToString(Application["log"]);
            logText = logText.Replace("\r\n", "<br>");
            log.Text = logText;
        }
    }
}