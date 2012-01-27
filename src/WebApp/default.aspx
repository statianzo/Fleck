<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="WebApp._default" %>
<%@ Import Namespace="System.Net.Mime" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
    <style type="text/css">
        div.col1 { float: left; width: 50%; }
        div.col2 { float: left; width: 50%; }
    </style>
</head>
<body>
    
    <div class="col1">
        <form id="form1" runat="server">
        <div>        
            <asp:ScriptManager runat="server" id="ScriptManager1">
            </asp:ScriptManager>
            <asp:updatepanel ID="Updatepanel1" runat="server">
                <ContentTemplate>
                    <h2>Demo Server</h2>
                    <asp:label runat="server" Id="log" text=""></asp:label>
                    <asp:timer ID="Timer1" runat="server" Interval="1000" OnTick="timer1_onTick" ></asp:timer>        
                </ContentTemplate>                        
            </asp:updatepanel>        
        </div>
        </form>
    </div>

    <div class="col2">
        <h2>Demo Client</h2>
        <form id="sendForm">
		    <input id="sendText" placeholder="Text to send" />
	    </form>
        <pre id="incomming"></pre>
    </div>
    <script type="text/javascript">
        var start = function () {
            var inc = document.getElementById('incomming');
            var wsImpl = window.WebSocket || window.MozWebSocket;
            var form = document.getElementById('sendForm');
            var input = document.getElementById('sendText');

            inc.innerHTML += "connecting to server ..<br/>";

            // create a new websocket and connect
            window.ws = new wsImpl('ws://localhost:8181', 'my-protocol');

            // when data is comming from the server, this metod is called
            ws.onmessage = function (evt) {
                inc.innerHTML += evt.data + '<br/>';
            };

            // when the connection is established, this method is called
            ws.onopen = function () {
                inc.innerHTML += '.. connection open<br/>';
            };

            // when the connection is closed, this method is called
            ws.onclose = function () {
                inc.innerHTML += '.. connection closed<br/>';
            }

            form.addEventListener('submit', function (e) {
                e.preventDefault();
                var val = input.value;
                ws.send(val);
                //input.value = "";
            });

        }
        window.onload = start;
    </script>

</body>
</html>
