using NUnit.Framework;
using System.Diagnostics;
using System.Text;

namespace Fleck.Tests
{
    [TestFixtureAttribute]
    public class RequestParserTests
    {
        [Test]
        public void ShouldReturnNullForEmptyBytes()
        {
            WebSocketHttpRequest request = RequestParser.Parse(new byte[0]);

            Assert.IsNull(request);
        }

        [Test]
        public void ShouldReadResourceLine()
        {
            WebSocketHttpRequest request = RequestParser.Parse(ValidRequestArray());

            Assert.AreEqual("GET", request.Method);
            Assert.AreEqual("/demo", request.Path);
        }

        [Test]
        public void ShouldReadHeaders()
        {
            WebSocketHttpRequest request = RequestParser.Parse(ValidRequestArray());

            Assert.AreEqual("example.com", request.Headers["Host"]);
            Assert.AreEqual("Upgrade", request.Headers["Connection"]);
            Assert.AreEqual("12998 5 Y3 1  .P00", request.Headers["Sec-WebSocket-Key2"]);
            Assert.AreEqual("http://example.com", request.Headers["Origin"]);
        }

        [Test]
        public void ShouldReadBody()
        {
            WebSocketHttpRequest request = RequestParser.Parse(ValidRequestArray());

            Assert.AreEqual("^n:ds[4U", request.Body);
        }

        [Test]
        public void ValidRequestShouldNotBeNull()
        {
            Assert.NotNull(RequestParser.Parse(ValidRequestArray()));
        }

        [Test]
        public void NoBodyRequestShouldNotBeNull()
        {
            const string noBodyRequest =
        "GET /demo HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
                "Sec-WebSocket-Protocol: sample\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n" +
                "Origin: http://example.com\r\n" +
                "\r\n" +
                "";
            var bytes = RequestArray(noBodyRequest);

            Assert.IsNotNull(RequestParser.Parse(bytes));
        }

        [Test]
        public void NoHeadersRequestShouldBeNull()
        {
            const string noHeadersNoBodyRequest =
        "GET /zing HTTP/1.1\r\n" +
                "\r\n" +
                "";
            var bytes = RequestArray(noHeadersNoBodyRequest);

            Assert.IsNull(RequestParser.Parse(bytes));
        }

        [Test]
        public void HeadersShouldBeCaseInsensitive()
        {
            WebSocketHttpRequest request = RequestParser.Parse(ValidRequestArray());

            Assert.IsTrue(request.Headers.ContainsKey("Sec-WebSocket-Protocol"));
            Assert.IsTrue(request.Headers.ContainsKey("sec-websocket-protocol"));
            Assert.IsTrue(request.Headers.ContainsKey("sec-WEBsocket-protoCOL"));
            Assert.IsTrue(request.Headers.ContainsKey("UPGRADE"));
            Assert.IsTrue(request.Headers.ContainsKey("CONNectiON"));
        }

        [Test]
        public void PartialHeaderRequestShouldBeNull()
        {
            const string partialHeaderRequest =
        "GET /demo HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
                "Sec-WebSocket-Protocol: sample\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Sec-WebSoc"; //Cut off
            var bytes = RequestArray(partialHeaderRequest);

            Assert.IsNull(RequestParser.Parse(bytes));
        }

        [Test]
        public void EmptyHeaderValuesShouldParse()
        {
            const string emptyCookieRequest =
                "GET /demo HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
                "Sec-WebSocket-Protocol: sample\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n" +
                "Origin: http://example.com\r\n" +
                "Cookie: \r\n" +
                "User-Agent:\r\n" +     //no space after colon
                "\r\n" +
                "^n:ds[4U";
            var bytes = RequestArray(emptyCookieRequest);
            var request = RequestParser.Parse(bytes);
            Assert.IsNotNull(request);
            Assert.AreEqual("", request.Headers["Cookie"]);
        }

        [Test]
        public void RunTimeOfParseRequestWithLargeCookie()
        {
            var totalTime = 0L;
            var watch = new Stopwatch();
            var largeHeaders = LargeHeaders();
            for (int i = 0; i < largeHeaders.Length; i++)
            {
                var header = largeHeaders[i];
                var bytes = RequestArray(header);
                watch.Reset();
                watch.Start();
                var conn = RequestParser.Parse(bytes);
                watch.Stop();
                totalTime += watch.ElapsedMilliseconds;
            }
            //_requestWithLargeCookie1 takes 1213 Milliseconds
            //_requestWithLargeCookie1 takes 2444 Milliseconds
            //_requestWithLargeCookie1 takes 3655 Milliseconds
            Assert.Less(totalTime, 100 * largeHeaders.Length);
        }

        public byte[] ValidRequestArray()
        {
            return RequestArray(validRequest);
        }

        public byte[] RequestArray(string request)
        {
            return Encoding.UTF8.GetBytes(request);
        }

        private string[] LargeHeaders()
        {
            return new string[] { _requestWithLargeCookie1, _requestWithLargeCookie2, _requestWithLargeCookie3 };
        }

        const string validRequest =
"GET /demo HTTP/1.1\r\n" +
            "Host: example.com\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
            "Sec-WebSocket-Protocol: sample\r\n" +
            "Upgrade: WebSocket\r\n" +
            "Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n" +
            "Origin: http://example.com\r\n" +
            "\r\n" +
            "^n:ds[4U";

        private const string _requestWithLargeCookie1 = @"GET / HTTP/1.1
Host: 192.168.1.1:8181
Connection: Upgrade
Pragma: no-cache
Cache-Control: no-cache
Upgrade: websocket
Origin: http://192.168.1.1:8000
Sec-WebSocket-Version: 13
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36
Accept-Encoding: gzip, deflate
Accept-Language: zh-CN,zh;q=0.9
Cookie: .AspNetCore.Cookies=chunks-4; .AspNetCore.CookiesC1=CfDJ8FQ5Nbx0MjtOkhtbrGABpUTBCLQzn80-tkmp9tF81sgz7u5dSaYter8FbI-9tYF98S5Fjgdc6liVQduVC0mvN-Po6POE8XKrhoQ-3o014Jz249qeFDOd-7j4PsfOAxWhSwf02DlXkKU6eQCl1qEYuhDZFRTEjXZCiIl9k64bY2nMmeGSPvO7_mNKjBVGxINT94CDFI8Xtb26QHIEKSIhFM1z8JCcs_6HTJ0ASCvrDoVI6nMmb1goaftihsQYhTaocNA9f0284tJNXqW8VSnBh1NbsRTVNealgmsySooBwmDdpYZpVOVqN4BFX1GN_8veUImdBBvq1P_EGppOsSFrTh2q8npKROpztTxQ234wKSiH-Cs9RT_NS3cnIwYi1u0wSPVUqiDCSnTNYdYwpTXXPdt4H5-HPcB8nUDCgrJyNyq5wnVKCZhkbVEpJ_ZQOiMQ4Bw1h6xfvif2JKgf_0yhww6UTx6hon_6X4jzTNthogMGI9qO66RG9So9Qs6ze8H2SgnU4DxzL_VJh3MEesZGKpjaLC6_tvwqWRmNAcoRkCDV6HgR8oIGp3zdXidzlWDK_arltgN8reuDRbvEhcniLrVY0Z0___aag5joC0_TYsTcT6D1BVyc-XGnjBEvA9cV3m1qHHh83lxNPJTsqlEqbwleFpas0DVU-3VfnMQjsycsvcnQxvujwf2AKvmT_4WIYV7UBmXnrxHrfwje4CMxotwgdCn_HBQlj84GVQNqan0yD4r4oWYWWV-BTewWJL2Atdszh0CATGnpTlkcI_iT15XxbpwWvqnlO9hWAY6W_gJsZP6wI0zFan2NKPs-EoKU9oaY0OaYoGvikpqKRrnCu6gpA8ErAFtm94-fCEa5w6YbOyx1pIGTatzZnwjhGhEG4Gv7vZo77tPPL84tm7PXExURlRNhw--ycsBy4jLPXo6MqbrjW7-kNb67hUA7sDiJAPO4WmnJzBYS1Yl94LJQCOsE3cMnxeXs9koGrIXnmgnRU_vRavl1G8wwmDUa1V8eA1TyWEwuIdZ1v12mBjWuCE8Z_tcStfmHCuacXfkiMK4ciajVvXwqx2mXbkrQu_3qL0-NxnpGHaa7lVBV7xsxPdlnEmJi0paMqrn864shFek4E1f-w83TBL0pviH9EhLo6hdNtdBAzMwJRGOKJ_MHIjjrEExaI4kVhyocHFHX1VGCVCdPMAEQv77pHS902GhK1jqiKUtaLrZUQB1tDPxp6-02Yg4VbswoiCWeNUEyfhir4f4Xk3fh_fEDf6IPcuM39XtEhqfFJCKXASB4XvNlnlBPtlG3XejbybGV_Zlk9bxB9HNgG7Zr4ym5_zddpMod99XV1Iml7WL32n2m5oGzvRPVufaZhEzTxdwsaHzGzMpE6bYiMlOkWDO-vTq7F_Vi-Rk9IaVUiPwS8G6JZ_G0jhqA15fiG1aYNi28pcPNQz3VunV3HsUiZR4q9lsfTFGfHlXq470pg-LkRBPOHjzdsDPW-WnpNGyU4EJmJ9oKWN-TmrYEw3alD9cXv4CI_DIlzy9RPxaUYdm3Vjvgg4W7Phv33eiuPzQ9RjStzkL7jWRppV11GCwgJUaTxyg7wnf62ADz2oS4j7hR-eGdMq5YgKw_h3JQCgQNn1egcAUjNE1aZRxMMafgVbGcMqVZuozE1edXjWxs3C5u8vebXcasxofo_cKMShU72jcxz-YU0QpqnVHxnTUSvQUo09wZ_1FEIzL0pwhDUSyS_ZZX2WM8AoGptguJME4fSR3SBxVFb92VACP2Y3wUxVjw4sUl4hMwmVdCIT-LP4nfAXFLnraOKy9O3Yq_VBHR4iCPtYxEWNn9_iop2O2-ZBr5OHjv2Heemkem9H37-_Vu9qhESJQfW_q4wjCp-gB0BWHMYKBMptK_knrgZSPgZuKgmExryGfqokNYWYrQ-upDszIK87Pi934nwJDhZujWOG34_93j9cOJHkB4p5DHcbyf0R__Aio5Tmd8b7IThNucWzzQCIk4yatXQSs4NCyRaMApGyhCGKySgFZx1BOqCrAO82fnUykcaxWDqRjf-YVVZtkLrmmjjRkiQ_6FPKhBWhzqrXEkuFvBzQYgeOEy2YAVKDsL7947swenld2wLolZBgy2xvIjklM09Ph1PHeekEOmx6xFVUIANOrHSUKAVWy7FMGugbZCHTf1Ay33KXS03SJHz7G5VBbidx65j8cH3mYCMYXb1gqxq_uX8os7guBwoAhcLcmBJyIoR0FKbtZ01eOHskN7kT-Fvp8Ri1QB2kdOKWrQM2D7eqvARLrpUXc6IT6XlcMB0paYB0VwKShMDDiB3dfS2de-sRiXdEjiPo-yMIF4UNGEvWnPZQbtC6im4EhnSLnqMP76umBeLdZdSkQaiXWt0WFcpYhmLbrn0cWnxQriH2e_iidZa9ROwLhumuqd7M62dNyZgQZZB-DLB7xUJ4N_xtYUgLF82ldAOMiYvPZozJrCVZoVaUlr7QKbxS5NuVHdmKY-8fdq1l_4HUPt2EJK8kX2dXxZXqWEOsRQaSQL7lbWnFW0umNyM95UTM6kdgTbBoArzb0tkwtchXLohr2pW57EXVIuTyUMlAjDvVnAzYTTfe_9-cfiC4LBF1tHd2QJYdnQSMVbIBH84s6InbSSvL5E2ByO6e1XHs3uterl4ootastj6esSRU_bN1R1I3QmGqfOjs-41eA225BgSWtGlJg5bkFuwvw5mvjFMt4xmmD9DWB7WW6vkQwEzqj1stH341lKb9tsPD3aKS_yLmGiY6bB0l3me4_xTWgmHPov6xlK4_0RqNuRBnCAoLTYbKsiQ-PRZC75Kvn8s6IoxvtEaqKaIdD93yRadITElS45O8F2rq8hClWdBaIgpgutyqf4jfJSbUmceeq7eJ3UFkRK_m1SnWxcmFDl4Eb2Lb7gm9Y3GP2UanSnYIbjoD3Yp6K0Llu3pNRtLvXlxvov3ksT5S3zVkiaaURZkMnp2kN0fP3lrekCA6obO6i7kzNRlnXRSB3xqCeblmBGwSOa0X4fIC8t5o0_R09q1lbKturrpWiR9jACeuzjjEzm1LwXaIbXCyIC3O3h0NQHEJzsT8xK5ksX1sBbwAQ5O7dpgejFxMU1B1SQ_lJTN63z_ritASK5w4H6IMGmfnuHp8k_Zzt5t4L2FmExeDCvoDSG6bRvsdbmkuk8eO_ecZTGJQHzVxmReo-9uagVY8-rfIkooLtmnrRhFfHpmfXAFvHGWUU0VVtUay__v4rN-dzuL7Esx_OyAGVDwuRqUzPnqXR8u3wdgF1eqKBzkIjBX0r-OmY6L957OcgDiCWBsWQvuBKbya_7UKRMiPgPdkL_b2BtzeVuuFh9JpcjhkoiahMUACxJoZeoBgE5nC8nPuAi2_GN6MrHYJfljaivxP0hSv8SqV4bZT9xlTTePfgFR842xOqCJh0_4xkMe6UCSSJQbS71ELFADiISoZraqeLWe5MIXbNAdSk52gSWwjIgE6b3usRLtxUVZxrO0vBTG0Uw-o_CvLBTyMCeOIVmy3lA-ifyQ7UGeX0i_hRbwcZkfl-Q4uAeqw7Ts2qwEviyCB24d46BiX4hD1jKNH2HEVZyrwHCDkTEW9_8twc2dwl0xlSYkXBttQC6WsQbwFdcBUHcAWV92Q2VFKr1Hk9m6f";

        private const string _requestWithLargeCookie2 = @"GET / HTTP/1.1
Host: 192.168.1.1:8181
Connection: Upgrade
Pragma: no-cache
Cache-Control: no-cache
Upgrade: websocket
Origin: http://192.168.1.1:8000
Sec-WebSocket-Version: 13
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36
Accept-Encoding: gzip, deflate
Accept-Language: zh-CN,zh;q=0.9
Cookie: .AspNetCore.Cookies=chunks-4; .AspNetCore.CookiesC1=CfDJ8FQ5Nbx0MjtOkhtbrGABpUTBCLQzn80-tkmp9tF81sgz7u5dSaYter8FbI-9tYF98S5Fjgdc6liVQduVC0mvN-Po6POE8XKrhoQ-3o014Jz249qeFDOd-7j4PsfOAxWhSwf02DlXkKU6eQCl1qEYuhDZFRTEjXZCiIl9k64bY2nMmeGSPvO7_mNKjBVGxINT94CDFI8Xtb26QHIEKSIhFM1z8JCcs_6HTJ0ASCvrDoVI6nMmb1goaftihsQYhTaocNA9f0284tJNXqW8VSnBh1NbsRTVNealgmsySooBwmDdpYZpVOVqN4BFX1GN_8veUImdBBvq1P_EGppOsSFrTh2q8npKROpztTxQ234wKSiH-Cs9RT_NS3cnIwYi1u0wSPVUqiDCSnTNYdYwpTXXPdt4H5-HPcB8nUDCgrJyNyq5wnVKCZhkbVEpJ_ZQOiMQ4Bw1h6xfvif2JKgf_0yhww6UTx6hon_6X4jzTNthogMGI9qO66RG9So9Qs6ze8H2SgnU4DxzL_VJh3MEesZGKpjaLC6_tvwqWRmNAcoRkCDV6HgR8oIGp3zdXidzlWDK_arltgN8reuDRbvEhcniLrVY0Z0___aag5joC0_TYsTcT6D1BVyc-XGnjBEvA9cV3m1qHHh83lxNPJTsqlEqbwleFpas0DVU-3VfnMQjsycsvcnQxvujwf2AKvmT_4WIYV7UBmXnrxHrfwje4CMxotwgdCn_HBQlj84GVQNqan0yD4r4oWYWWV-BTewWJL2Atdszh0CATGnpTlkcI_iT15XxbpwWvqnlO9hWAY6W_gJsZP6wI0zFan2NKPs-EoKU9oaY0OaYoGvikpqKRrnCu6gpA8ErAFtm94-fCEa5w6YbOyx1pIGTatzZnwjhGhEG4Gv7vZo77tPPL84tm7PXExURlRNhw--ycsBy4jLPXo6MqbrjW7-kNb67hUA7sDiJAPO4WmnJzBYS1Yl94LJQCOsE3cMnxeXs9koGrIXnmgnRU_vRavl1G8wwmDUa1V8eA1TyWEwuIdZ1v12mBjWuCE8Z_tcStfmHCuacXfkiMK4ciajVvXwqx2mXbkrQu_3qL0-NxnpGHaa7lVBV7xsxPdlnEmJi0paMqrn864shFek4E1f-w83TBL0pviH9EhLo6hdNtdBAzMwJRGOKJ_MHIjjrEExaI4kVhyocHFHX1VGCVCdPMAEQv77pHS902GhK1jqiKUtaLrZUQB1tDPxp6-02Yg4VbswoiCWeNUEyfhir4f4Xk3fh_fEDf6IPcuM39XtEhqfFJCKXASB4XvNlnlBPtlG3XejbybGV_Zlk9bxB9HNgG7Zr4ym5_zddpMod99XV1Iml7WL32n2m5oGzvRPVufaZhEzTxdwsaHzGzMpE6bYiMlOkWDO-vTq7F_Vi-Rk9IaVUiPwS8G6JZ_G0jhqA15fiG1aYNi28pcPNQz3VunV3HsUiZR4q9lsfTFGfHlXq470pg-LkRBPOHjzdsDPW-WnpNGyU4EJmJ9oKWN-TmrYEw3alD9cXv4CI_DIlzy9RPxaUYdm3Vjvgg4W7Phv33eiuPzQ9RjStzkL7jWRppV11GCwgJUaTxyg7wnf62ADz2oS4j7hR-eGdMq5YgKw_h3JQCgQNn1egcAUjNE1aZRxMMafgVbGcMqVZuozE1edXjWxs3C5u8vebXcasxofo_cKMShU72jcxz-YU0QpqnVHxnTUSvQUo09wZ_1FEIzL0pwhDUSyS_ZZX2WM8AoGptguJME4fSR3SBxVFb92VACP2Y3wUxVjw4sUl4hMwmVdCIT-LP4nfAXFLnraOKy9O3Yq_VBHR4iCPtYxEWNn9_iop2O2-ZBr5OHjv2Heemkem9H37-_Vu9qhESJQfW_q4wjCp-gB0BWHMYKBMptK_knrgZSPgZuKgmExryGfqokNYWYrQ-upDszIK87Pi934nwJDhZujWOG34_93j9cOJHkB4p5DHcbyf0R__Aio5Tmd8b7IThNucWzzQCIk4yatXQSs4NCyRaMApGyhCGKySgFZx1BOqCrAO82fnUykcaxWDqRjf-YVVZtkLrmmjjRkiQ_6FPKhBWhzqrXEkuFvBzQYgeOEy2YAVKDsL7947swenld2wLolZBgy2xvIjklM09Ph1PHeekEOmx6xFVUIANOrHSUKAVWy7FMGugbZCHTf1Ay33KXS03SJHz7G5VBbidx65j8cH3mYCMYXb1gqxq_uX8os7guBwoAhcLcmBJyIoR0FKbtZ01eOHskN7kT-Fvp8Ri1QB2kdOKWrQM2D7eqvARLrpUXc6IT6XlcMB0paYB0VwKShMDDiB3dfS2de-sRiXdEjiPo-yMIF4UNGEvWnPZQbtC6im4EhnSLnqMP76umBeLdZdSkQaiXWt0WFcpYhmLbrn0cWnxQriH2e_iidZa9ROwLhumuqd7M62dNyZgQZZB-DLB7xUJ4N_xtYUgLF82ldAOMiYvPZozJrCVZoVaUlr7QKbxS5NuVHdmKY-8fdq1l_4HUPt2EJK8kX2dXxZXqWEOsRQaSQL7lbWnFW0umNyM95UTM6kdgTbBoArzb0tkwtchXLohr2pW57EXVIuTyUMlAjDvVnAzYTTfe_9-cfiC4LBF1tHd2QJYdnQSMVbIBH84s6InbSSvL5E2ByO6e1XHs3uterl4ootastj6esSRU_bN1R1I3QmGqfOjs-41eA225BgSWtGlJg5bkFuwvw5mvjFMt4xmmD9DWB7WW6vkQwEzqj1stH341lKb9tsPD3aKS_yLmGiY6bB0l3me4_xTWgmHPov6xlK4_0RqNuRBnCAoLTYbKsiQ-PRZC75Kvn8s6IoxvtEaqKaIdD93yRadITElS45O8F2rq8hClWdBaIgpgutyqf4jfJSbUmceeq7eJ3UFkRK_m1SnWxcmFDl4Eb2Lb7gm9Y3GP2UanSnYIbjoD3Yp6K0Llu3pNRtLvXlxvov3ksT5S3zVkiaaURZkMnp2kN0fP3lrekCA6obO6i7kzNRlnXRSB3xqCeblmBGwSOa0X4fIC8t5o0_R09q1lbKturrpWiR9jACeuzjjEzm1LwXaIbXCyIC3O3h0NQHEJzsT8xK5ksX1sBbwAQ5O7dpgejFxMU1B1SQ_lJTN63z_ritASK5w4H6IMGmfnuHp8k_Zzt5t4L2FmExeDCvoDSG6bRvsdbmkuk8eO_ecZTGJQHzVxmReo-9uagVY8-rfIkooLtmnrRhFfHpmfXAFvHGWUU0VVtUay__v4rN-dzuL7Esx_OyAGVDwuRqUzPnqXR8u3wdgF1eqKBzkIjBX0r-OmY6L957OcgDiCWBsWQvuBKbya_7UKRMiPgPdkL_b2BtzeVuuFh9JpcjhkoiahMUACxJoZeoBgE5nC8nPuAi2_GN6MrHYJfljaivxP0hSv8SqV4bZT9xlTTePfgFR842xOqCJh0_4xkMe6UCSSJQbS71ELFADiISoZraqeLWe5MIXbNAdSk52gSWwjIgE6b3usRLtxUVZxrO0vBTG0Uw-o_CvLBTyMCeOIVmy3lA-ifyQ7UGeX0i_hRbwcZkfl-Q4uAeqw7Ts2qwEviyCB24d46BiX4hD1jKNH2HEVZyrwHCDkTEW9_8twc2dwl0xlSYkXBttQC6WsQbwFdcBUHcAWV92Q2VFKr1Hk9m6fuwlZn3qvg1287uX0xx38onGOJ3LH_oBmgh9oECVVwXT6YcJUZFr1dZoBR8n-_x1PayO8M-1gkfouFd_UGAmbV7Kn0dpMIducmKti4x4oJDJ2nRb0lBBE2c3_Xu8Ffu-Ij52oLlW8nH5RTs1da5PURn_jmn38yTm1XcrkaE_pIK2wkilXz0OIL_qZQdhuHvv2xw_EKG8J6E1fR6JR7umarkOGorLzQHOfKp5sGbto0CQIbEOyguy5fTD2IlwsrZUCOuTLaUpI07wykupqNS954YSWK0mMMWPJRiuBjkK3V4YvVxu3qoVkVNZQNYt-09uVPqYXFVSDwRkEg; .AspNetCore.CookiesC2=VvU2F_TmeMeiVLKe3N9uk2KU7ahtciOVHhkbfSBzDEhaf6bXbfo8hPLdAx2UktsnXcRzpwQaUfyLO7Zop9M_Q9tcpb7YrFbPHFS2ihEkA7IgSsrpQf3G3a2bijDCuoM6avPIPBUUOUCgmcrFXWLWEZwoRGxPnoUNTtoQd11fdazlcBlxFhT_rDwsL-FasaX2UvGzA6oKIiS-6wioionH_Tq7ldHl-6Zs1iBY5ZB7O8Kvb1RTYyB1ka2Rden-M3jl829CJAXGg35IO2dAXJQOL6urFVIThCSrBP9ytf0YCuj3vO4FxeHJA0n0IkFzE1K8JMioGSzll1EG0wp_TB3yfAk9IPSokik2beDG45Dpj8-c3OojIErtEcZZZPe1VW9fDC7egnfCdIKH-ZC5-02QV6bk79pLT9UizRW30lYSuJe7VrBPs36cBtXr1ViNMNAi0Ehx2eB_sPWMH5thtW7XlogJ_UmYAIsmpfqROuRMmWyldFtzcf_w643EIi96qNYlaK3eH_qSrXfDYmVQvfJKjZpiRIga4cjEKx3CJAuQvcqifbnK-TkyCMPsdL8ADGZXsPZwvKqxx8AGKgMz65BddV9NrQqeI6abqjzCZhDcT16CB4AnOaQxOxv8fCEkfGSBTAzFcejelOHZj_BMmLS-W16CA0ltWT9pkVqGRCi-dfd9FBQPr7mPv9vgd-YOQXtAVEkjzXWewsLBWWe8fKc_6sMNpYRCnQQgStMhZ9ICMfIdguI7nBRFKY6X-HWL14TV2oXh_Q1ScvdWpwUH2vDCsyKGE27AfVtz5tWFanOxH7PZ4HBoM59hQ6XmY15jJ4uyWxzZyUHWKDQdDltzjRxoCa-HVdcOhEjA-q5M9Hb11pmZuB4L7s0ZDq_FdLcgf3efyBHBpzHD5grfzYVrg3Q1hILBg0WMqyrQixwhP5p3P0du3amaGLunoz-78JRxQjfdGVQn-BIgN1VoxfpB5ABpammjyQUo5L6XlPgebVizSiSPb9teWGWpbe7StE0is12FwE6IjUX4-dt62yZqg86avhP1eS596JhS2BdROYg-lZZhMgmPfuLKKvjRCkRLNgRCipF017xBnBOnLbM5FD1HFrP8LxMkL6H4XtVVsiZB0eWKiKKAVVHNtlpsF8kkysUEwKkeJkIMM4VjO_OhbntmxHMfcG4GNePL2ad90MYN0HaEO0L_WCaumIDSluva_nBdpPhd2oumjMcv5YVgj7tP1IjKtmnqV7gGZEdcNZIJyvKmBc-hEXpY9RpsOGlkxj8XfUyPs3egXtyRlFpH4FI2f2Jz4mWoG8lc2hbiZZSwUhSZuzfHSbPuQ10RQA_BRHSMDxWf99VMcjJCeqaqnlJTdxOMgjii1WDcqeqROFPcFOGYVwqIK1qmxfx_oSJB-r4KshbP6MQmC5XEYAZLPaCElLO8mtBsQ-x74n6QcnsVALyg8R6lB8LzCevVEQg3acEAQvI6SfPsCOTgMftlYE-79YzvIDkseRG1WuYivtUXZayEZkqPqQYjZcRiqHNKzMsQ9FeGT40gZtKneknW7OQKExG2IUQp4rvOx4LL0RqrLIIwFBL08uSK9OHRJmUNe3PBiYKKL-oy_MCMZY0Pfkty_U-dgu6J_HwwIkUCroercOQPqsgVGNHP1yjpQ66NYTMhXLCn7S74agSXrAmXZkXRbUJEsG2C8H7Vsm89HJKXnV9KxLEL2G2iZO61OZZSp9lmCcJcJd_ORgoJmHCkvkllM7nenAbJooLc3s-e4zYgkqeebgjgFzBlPEpRFpcbnSi4F72ixs4n_ellXJk5R3KPwsuM5WGfVTSJAt72Sz2-v1R3Wndia_RGe2VPlUXaLVVZ60TT4iJ_TgvtBSbsVhCYLkORFy_pvj2UaQdD0pHwQH_OJiN-nD3kL36gm1xaZlpo3JvTI8d0Ib1J-eGcyedCNtjqCwa0_06r-JWlxpWu3vOMzgOnSRHIDQtfRDk8WtYDHc--cbg2Ttarg4Dl7RaBu5Lb_9AYrIu6B1rsnrVPRxjI6Pt9RoJ9XgCFpgvHMXSgU7SoO8jf_j19fD851fRs31tmjE3Vqv3XONsfppFy2dc5GpGg__xS2P6E1c9bcXxdSu-tBPt2JSOk6KrqVP2_ud5fP1pGzwcTIqe48pStbOkaK75Nl5T_b6MNOCVqZ9LOytLVzj7JI74crmx7lCdB991D3QTe-ZAqBbIfTMpat04pvhDtDdIGVEWLDGVDpFBvQS7rHcjmetzUnFHKTn8MOOXta1toadMm-sihCUzWq3FbSrCu8R_S3s_VUPnAAYPtk1pRSYfiUQDgoVCir4IToisWXLCbA6g4lrnh7eczmA4UIpBqN7orJkJuunpfwyfA2AYiNYRvkZRTBODMzpg1c_DCddYmo5kihv4LsMv3yK4HdJnfGlFkxRD_nur0twMZze82wUrvOuUMc9m24AheTOJ_2DGBZ61i4tohgi6btStoSLjZsH1PFCunLi5fL0VokqAjMGacMOQPGq6FlDclIJ753NO2xLB6ELXrfzCAO7Fkt6W7oCkTz0IK8K_P7IxVA3Mn7hlSviZqiM1GVJZb5UF2CpzY1lNgdHDSm91R-LVE9NNvrreagF2M8hi_S3AOUdDHpHUEePce60MUXBYflbbfi-ZyoMA8pJV1hkoTpldHz8y2cec5B7plaAd4roFP-jxlNrcBKq_TA-GHJNvAZVXIPJHOBCtjnN06DMK9orpY7EqILuaCwF266Z9rujYoYRYgwL_I5ASu0w4L6fa1KGTjqF-b7935ndAjaNqS5nVvD6SmYFN2B7o-T0l7TWkCa-GTaARpNIjg6Js9Rp-CvR0o5BDYQycZ_hReBDGDXGFYaGH-bwJnNo4RV9Q38KTBDxamBjVz7AVz8xs-2b1OujUTArPStOhAREm9V64IGDq1fahUx0UspQpj4Q96cl8WZhMQPniI1K_FL21nmSOWPAYNLgHryuNoHI9XnKooh2hsy72sQV6ncyjfAh7XB2AUumfuGjulyW_Vzk4hFjJhebGVeywXpwHsARwRL5OlcDcFdS22T4BZ_xE47RzkNBLW6p7c67Jq2b_KRbB52xKsIJjKzwYc7ATxfciGoNfgjmB3lburppR4fTtD1Tcpvxlght_LYyeXFH3aopsUvVPebsaeLULXMXak4nMBrZZL8DXoFehv-mjEWaQoo09jPCzbntugrx46OFgTsz59-bhWnjTcfsSbHRU-wlqZo5IXw9r757PQPlQpnrm1iAJBn5u7MdjCgUz0l5cfBYDPc70-yG9Na27EXOEQktZLo5W5MnLkVf0fvMO8pjePMlRzoH77yHz7Zz0mZt-kk1D6WnrKuc3c1s3_8-cjbqpeZFjcFuOXQnc1MpWQEkbmpZSv8SkWyjw0KyyUlQ1O-uaVVxH4_Ie9t8khGyd5embExYXHZ4dri1BpC7MHAp8gM5L8cZyDAZ-4rJMzPYbRth7fwAtOu-q4ew7CNsxoQAhAG6YSWWh0PIgsm2GYzgeVNLO6SmPFX25lvxu77S8GGILx-hVKLl6BnAHw0H3Ej6Tr9QdKz6M4_ueE7svUm7bBuYh0qIzUXcjcD1QVslwNu7xdnePIN1rZDWApUsiPLWTMGzgOPHy0_wej2zd1jSnBzJM8YbjsCcN9sQDj0K9P7u2TnHO1E5d3Mm4qZUHePaDxctcsdN1-2295zoXKm4QvQ6aaFkyscO3dKVoCJIyO1PER_BcMVZWBdeq";

        private const string _requestWithLargeCookie3 = @"GET / HTTP/1.1
Host: 192.168.1.1:8181
Connection: Upgrade
Pragma: no-cache
Cache-Control: no-cache
Upgrade: websocket
Origin: http://192.168.1.1:8000
Sec-WebSocket-Version: 13
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36
Accept-Encoding: gzip, deflate
Accept-Language: zh-CN,zh;q=0.9
Cookie: .AspNetCore.Cookies=chunks-4; .AspNetCore.CookiesC1=CfDJ8FQ5Nbx0MjtOkhtbrGABpUTBCLQzn80-tkmp9tF81sgz7u5dSaYter8FbI-9tYF98S5Fjgdc6liVQduVC0mvN-Po6POE8XKrhoQ-3o014Jz249qeFDOd-7j4PsfOAxWhSwf02DlXkKU6eQCl1qEYuhDZFRTEjXZCiIl9k64bY2nMmeGSPvO7_mNKjBVGxINT94CDFI8Xtb26QHIEKSIhFM1z8JCcs_6HTJ0ASCvrDoVI6nMmb1goaftihsQYhTaocNA9f0284tJNXqW8VSnBh1NbsRTVNealgmsySooBwmDdpYZpVOVqN4BFX1GN_8veUImdBBvq1P_EGppOsSFrTh2q8npKROpztTxQ234wKSiH-Cs9RT_NS3cnIwYi1u0wSPVUqiDCSnTNYdYwpTXXPdt4H5-HPcB8nUDCgrJyNyq5wnVKCZhkbVEpJ_ZQOiMQ4Bw1h6xfvif2JKgf_0yhww6UTx6hon_6X4jzTNthogMGI9qO66RG9So9Qs6ze8H2SgnU4DxzL_VJh3MEesZGKpjaLC6_tvwqWRmNAcoRkCDV6HgR8oIGp3zdXidzlWDK_arltgN8reuDRbvEhcniLrVY0Z0___aag5joC0_TYsTcT6D1BVyc-XGnjBEvA9cV3m1qHHh83lxNPJTsqlEqbwleFpas0DVU-3VfnMQjsycsvcnQxvujwf2AKvmT_4WIYV7UBmXnrxHrfwje4CMxotwgdCn_HBQlj84GVQNqan0yD4r4oWYWWV-BTewWJL2Atdszh0CATGnpTlkcI_iT15XxbpwWvqnlO9hWAY6W_gJsZP6wI0zFan2NKPs-EoKU9oaY0OaYoGvikpqKRrnCu6gpA8ErAFtm94-fCEa5w6YbOyx1pIGTatzZnwjhGhEG4Gv7vZo77tPPL84tm7PXExURlRNhw--ycsBy4jLPXo6MqbrjW7-kNb67hUA7sDiJAPO4WmnJzBYS1Yl94LJQCOsE3cMnxeXs9koGrIXnmgnRU_vRavl1G8wwmDUa1V8eA1TyWEwuIdZ1v12mBjWuCE8Z_tcStfmHCuacXfkiMK4ciajVvXwqx2mXbkrQu_3qL0-NxnpGHaa7lVBV7xsxPdlnEmJi0paMqrn864shFek4E1f-w83TBL0pviH9EhLo6hdNtdBAzMwJRGOKJ_MHIjjrEExaI4kVhyocHFHX1VGCVCdPMAEQv77pHS902GhK1jqiKUtaLrZUQB1tDPxp6-02Yg4VbswoiCWeNUEyfhir4f4Xk3fh_fEDf6IPcuM39XtEhqfFJCKXASB4XvNlnlBPtlG3XejbybGV_Zlk9bxB9HNgG7Zr4ym5_zddpMod99XV1Iml7WL32n2m5oGzvRPVufaZhEzTxdwsaHzGzMpE6bYiMlOkWDO-vTq7F_Vi-Rk9IaVUiPwS8G6JZ_G0jhqA15fiG1aYNi28pcPNQz3VunV3HsUiZR4q9lsfTFGfHlXq470pg-LkRBPOHjzdsDPW-WnpNGyU4EJmJ9oKWN-TmrYEw3alD9cXv4CI_DIlzy9RPxaUYdm3Vjvgg4W7Phv33eiuPzQ9RjStzkL7jWRppV11GCwgJUaTxyg7wnf62ADz2oS4j7hR-eGdMq5YgKw_h3JQCgQNn1egcAUjNE1aZRxMMafgVbGcMqVZuozE1edXjWxs3C5u8vebXcasxofo_cKMShU72jcxz-YU0QpqnVHxnTUSvQUo09wZ_1FEIzL0pwhDUSyS_ZZX2WM8AoGptguJME4fSR3SBxVFb92VACP2Y3wUxVjw4sUl4hMwmVdCIT-LP4nfAXFLnraOKy9O3Yq_VBHR4iCPtYxEWNn9_iop2O2-ZBr5OHjv2Heemkem9H37-_Vu9qhESJQfW_q4wjCp-gB0BWHMYKBMptK_knrgZSPgZuKgmExryGfqokNYWYrQ-upDszIK87Pi934nwJDhZujWOG34_93j9cOJHkB4p5DHcbyf0R__Aio5Tmd8b7IThNucWzzQCIk4yatXQSs4NCyRaMApGyhCGKySgFZx1BOqCrAO82fnUykcaxWDqRjf-YVVZtkLrmmjjRkiQ_6FPKhBWhzqrXEkuFvBzQYgeOEy2YAVKDsL7947swenld2wLolZBgy2xvIjklM09Ph1PHeekEOmx6xFVUIANOrHSUKAVWy7FMGugbZCHTf1Ay33KXS03SJHz7G5VBbidx65j8cH3mYCMYXb1gqxq_uX8os7guBwoAhcLcmBJyIoR0FKbtZ01eOHskN7kT-Fvp8Ri1QB2kdOKWrQM2D7eqvARLrpUXc6IT6XlcMB0paYB0VwKShMDDiB3dfS2de-sRiXdEjiPo-yMIF4UNGEvWnPZQbtC6im4EhnSLnqMP76umBeLdZdSkQaiXWt0WFcpYhmLbrn0cWnxQriH2e_iidZa9ROwLhumuqd7M62dNyZgQZZB-DLB7xUJ4N_xtYUgLF82ldAOMiYvPZozJrCVZoVaUlr7QKbxS5NuVHdmKY-8fdq1l_4HUPt2EJK8kX2dXxZXqWEOsRQaSQL7lbWnFW0umNyM95UTM6kdgTbBoArzb0tkwtchXLohr2pW57EXVIuTyUMlAjDvVnAzYTTfe_9-cfiC4LBF1tHd2QJYdnQSMVbIBH84s6InbSSvL5E2ByO6e1XHs3uterl4ootastj6esSRU_bN1R1I3QmGqfOjs-41eA225BgSWtGlJg5bkFuwvw5mvjFMt4xmmD9DWB7WW6vkQwEzqj1stH341lKb9tsPD3aKS_yLmGiY6bB0l3me4_xTWgmHPov6xlK4_0RqNuRBnCAoLTYbKsiQ-PRZC75Kvn8s6IoxvtEaqKaIdD93yRadITElS45O8F2rq8hClWdBaIgpgutyqf4jfJSbUmceeq7eJ3UFkRK_m1SnWxcmFDl4Eb2Lb7gm9Y3GP2UanSnYIbjoD3Yp6K0Llu3pNRtLvXlxvov3ksT5S3zVkiaaURZkMnp2kN0fP3lrekCA6obO6i7kzNRlnXRSB3xqCeblmBGwSOa0X4fIC8t5o0_R09q1lbKturrpWiR9jACeuzjjEzm1LwXaIbXCyIC3O3h0NQHEJzsT8xK5ksX1sBbwAQ5O7dpgejFxMU1B1SQ_lJTN63z_ritASK5w4H6IMGmfnuHp8k_Zzt5t4L2FmExeDCvoDSG6bRvsdbmkuk8eO_ecZTGJQHzVxmReo-9uagVY8-rfIkooLtmnrRhFfHpmfXAFvHGWUU0VVtUay__v4rN-dzuL7Esx_OyAGVDwuRqUzPnqXR8u3wdgF1eqKBzkIjBX0r-OmY6L957OcgDiCWBsWQvuBKbya_7UKRMiPgPdkL_b2BtzeVuuFh9JpcjhkoiahMUACxJoZeoBgE5nC8nPuAi2_GN6MrHYJfljaivxP0hSv8SqV4bZT9xlTTePfgFR842xOqCJh0_4xkMe6UCSSJQbS71ELFADiISoZraqeLWe5MIXbNAdSk52gSWwjIgE6b3usRLtxUVZxrO0vBTG0Uw-o_CvLBTyMCeOIVmy3lA-ifyQ7UGeX0i_hRbwcZkfl-Q4uAeqw7Ts2qwEviyCB24d46BiX4hD1jKNH2HEVZyrwHCDkTEW9_8twc2dwl0xlSYkXBttQC6WsQbwFdcBUHcAWV92Q2VFKr1Hk9m6fuwlZn3qvg1287uX0xx38onGOJ3LH_oBmgh9oECVVwXT6YcJUZFr1dZoBR8n-_x1PayO8M-1gkfouFd_UGAmbV7Kn0dpMIducmKti4x4oJDJ2nRb0lBBE2c3_Xu8Ffu-Ij52oLlW8nH5RTs1da5PURn_jmn38yTm1XcrkaE_pIK2wkilXz0OIL_qZQdhuHvv2xw_EKG8J6E1fR6JR7umarkOGorLzQHOfKp5sGbto0CQIbEOyguy5fTD2IlwsrZUCOuTLaUpI07wykupqNS954YSWK0mMMWPJRiuBjkK3V4YvVxu3qoVkVNZQNYt-09uVPqYXFVSDwRkEg; .AspNetCore.CookiesC2=VvU2F_TmeMeiVLKe3N9uk2KU7ahtciOVHhkbfSBzDEhaf6bXbfo8hPLdAx2UktsnXcRzpwQaUfyLO7Zop9M_Q9tcpb7YrFbPHFS2ihEkA7IgSsrpQf3G3a2bijDCuoM6avPIPBUUOUCgmcrFXWLWEZwoRGxPnoUNTtoQd11fdazlcBlxFhT_rDwsL-FasaX2UvGzA6oKIiS-6wioionH_Tq7ldHl-6Zs1iBY5ZB7O8Kvb1RTYyB1ka2Rden-M3jl829CJAXGg35IO2dAXJQOL6urFVIThCSrBP9ytf0YCuj3vO4FxeHJA0n0IkFzE1K8JMioGSzll1EG0wp_TB3yfAk9IPSokik2beDG45Dpj8-c3OojIErtEcZZZPe1VW9fDC7egnfCdIKH-ZC5-02QV6bk79pLT9UizRW30lYSuJe7VrBPs36cBtXr1ViNMNAi0Ehx2eB_sPWMH5thtW7XlogJ_UmYAIsmpfqROuRMmWyldFtzcf_w643EIi96qNYlaK3eH_qSrXfDYmVQvfJKjZpiRIga4cjEKx3CJAuQvcqifbnK-TkyCMPsdL8ADGZXsPZwvKqxx8AGKgMz65BddV9NrQqeI6abqjzCZhDcT16CB4AnOaQxOxv8fCEkfGSBTAzFcejelOHZj_BMmLS-W16CA0ltWT9pkVqGRCi-dfd9FBQPr7mPv9vgd-YOQXtAVEkjzXWewsLBWWe8fKc_6sMNpYRCnQQgStMhZ9ICMfIdguI7nBRFKY6X-HWL14TV2oXh_Q1ScvdWpwUH2vDCsyKGE27AfVtz5tWFanOxH7PZ4HBoM59hQ6XmY15jJ4uyWxzZyUHWKDQdDltzjRxoCa-HVdcOhEjA-q5M9Hb11pmZuB4L7s0ZDq_FdLcgf3efyBHBpzHD5grfzYVrg3Q1hILBg0WMqyrQixwhP5p3P0du3amaGLunoz-78JRxQjfdGVQn-BIgN1VoxfpB5ABpammjyQUo5L6XlPgebVizSiSPb9teWGWpbe7StE0is12FwE6IjUX4-dt62yZqg86avhP1eS596JhS2BdROYg-lZZhMgmPfuLKKvjRCkRLNgRCipF017xBnBOnLbM5FD1HFrP8LxMkL6H4XtVVsiZB0eWKiKKAVVHNtlpsF8kkysUEwKkeJkIMM4VjO_OhbntmxHMfcG4GNePL2ad90MYN0HaEO0L_WCaumIDSluva_nBdpPhd2oumjMcv5YVgj7tP1IjKtmnqV7gGZEdcNZIJyvKmBc-hEXpY9RpsOGlkxj8XfUyPs3egXtyRlFpH4FI2f2Jz4mWoG8lc2hbiZZSwUhSZuzfHSbPuQ10RQA_BRHSMDxWf99VMcjJCeqaqnlJTdxOMgjii1WDcqeqROFPcFOGYVwqIK1qmxfx_oSJB-r4KshbP6MQmC5XEYAZLPaCElLO8mtBsQ-x74n6QcnsVALyg8R6lB8LzCevVEQg3acEAQvI6SfPsCOTgMftlYE-79YzvIDkseRG1WuYivtUXZayEZkqPqQYjZcRiqHNKzMsQ9FeGT40gZtKneknW7OQKExG2IUQp4rvOx4LL0RqrLIIwFBL08uSK9OHRJmUNe3PBiYKKL-oy_MCMZY0Pfkty_U-dgu6J_HwwIkUCroercOQPqsgVGNHP1yjpQ66NYTMhXLCn7S74agSXrAmXZkXRbUJEsG2C8H7Vsm89HJKXnV9KxLEL2G2iZO61OZZSp9lmCcJcJd_ORgoJmHCkvkllM7nenAbJooLc3s-e4zYgkqeebgjgFzBlPEpRFpcbnSi4F72ixs4n_ellXJk5R3KPwsuM5WGfVTSJAt72Sz2-v1R3Wndia_RGe2VPlUXaLVVZ60TT4iJ_TgvtBSbsVhCYLkORFy_pvj2UaQdD0pHwQH_OJiN-nD3kL36gm1xaZlpo3JvTI8d0Ib1J-eGcyedCNtjqCwa0_06r-JWlxpWu3vOMzgOnSRHIDQtfRDk8WtYDHc--cbg2Ttarg4Dl7RaBu5Lb_9AYrIu6B1rsnrVPRxjI6Pt9RoJ9XgCFpgvHMXSgU7SoO8jf_j19fD851fRs31tmjE3Vqv3XONsfppFy2dc5GpGg__xS2P6E1c9bcXxdSu-tBPt2JSOk6KrqVP2_ud5fP1pGzwcTIqe48pStbOkaK75Nl5T_b6MNOCVqZ9LOytLVzj7JI74crmx7lCdB991D3QTe-ZAqBbIfTMpat04pvhDtDdIGVEWLDGVDpFBvQS7rHcjmetzUnFHKTn8MOOXta1toadMm-sihCUzWq3FbSrCu8R_S3s_VUPnAAYPtk1pRSYfiUQDgoVCir4IToisWXLCbA6g4lrnh7eczmA4UIpBqN7orJkJuunpfwyfA2AYiNYRvkZRTBODMzpg1c_DCddYmo5kihv4LsMv3yK4HdJnfGlFkxRD_nur0twMZze82wUrvOuUMc9m24AheTOJ_2DGBZ61i4tohgi6btStoSLjZsH1PFCunLi5fL0VokqAjMGacMOQPGq6FlDclIJ753NO2xLB6ELXrfzCAO7Fkt6W7oCkTz0IK8K_P7IxVA3Mn7hlSviZqiM1GVJZb5UF2CpzY1lNgdHDSm91R-LVE9NNvrreagF2M8hi_S3AOUdDHpHUEePce60MUXBYflbbfi-ZyoMA8pJV1hkoTpldHz8y2cec5B7plaAd4roFP-jxlNrcBKq_TA-GHJNvAZVXIPJHOBCtjnN06DMK9orpY7EqILuaCwF266Z9rujYoYRYgwL_I5ASu0w4L6fa1KGTjqF-b7935ndAjaNqS5nVvD6SmYFN2B7o-T0l7TWkCa-GTaARpNIjg6Js9Rp-CvR0o5BDYQycZ_hReBDGDXGFYaGH-bwJnNo4RV9Q38KTBDxamBjVz7AVz8xs-2b1OujUTArPStOhAREm9V64IGDq1fahUx0UspQpj4Q96cl8WZhMQPniI1K_FL21nmSOWPAYNLgHryuNoHI9XnKooh2hsy72sQV6ncyjfAh7XB2AUumfuGjulyW_Vzk4hFjJhebGVeywXpwHsARwRL5OlcDcFdS22T4BZ_xE47RzkNBLW6p7c67Jq2b_KRbB52xKsIJjKzwYc7ATxfciGoNfgjmB3lburppR4fTtD1Tcpvxlght_LYyeXFH3aopsUvVPebsaeLULXMXak4nMBrZZL8DXoFehv-mjEWaQoo09jPCzbntugrx46OFgTsz59-bhWnjTcfsSbHRU-wlqZo5IXw9r757PQPlQpnrm1iAJBn5u7MdjCgUz0l5cfBYDPc70-yG9Na27EXOEQktZLo5W5MnLkVf0fvMO8pjePMlRzoH77yHz7Zz0mZt-kk1D6WnrKuc3c1s3_8-cjbqpeZFjcFuOXQnc1MpWQEkbmpZSv8SkWyjw0KyyUlQ1O-uaVVxH4_Ie9t8khGyd5embExYXHZ4dri1BpC7MHAp8gM5L8cZyDAZ-4rJMzPYbRth7fwAtOu-q4ew7CNsxoQAhAG6YSWWh0PIgsm2GYzgeVNLO6SmPFX25lvxu77S8GGILx-hVKLl6BnAHw0H3Ej6Tr9QdKz6M4_ueE7svUm7bBuYh0qIzUXcjcD1QVslwNu7xdnePIN1rZDWApUsiPLWTMGzgOPHy0_wej2zd1jSnBzJM8YbjsCcN9sQDj0K9P7u2TnHO1E5d3Mm4qZUHePaDxctcsdN1-2295zoXKm4QvQ6aaFkyscO3dKVoCJIyO1PER_BcMVZWBdeq1jxGTOFVVtlOzfvS0R7-68nr68sV9hCVo1h3LpQuab-gNw4CZa9gztGW9unCJ9X0uyNvrLQos7fOxGAmxO-zBwqc2bO3lljoPMCXwLB70XmfJk_lN9-BYNjQwPPUWxjSZ1lgKZlIQYVi6iDG2J-AE6kQCwG49gDmWuvCsYQ1ml0sSLkGIC_4PSpFbP1xHiMaT5KK3ez9YMhs7mAXs2o8sx2DKMLBUux_NxJt0UH4OH8VATOaxZbTL4fJ-7cKJeut; .AspNetCore.CookiesC3=3d9YY1oBDKCky4SGUNNC7xUuwOZNB84RKMyhQGiCzuyjq74Z7IW3EUvk_2HqZGZTSbAqOHCTp3Z4Yy7CNA66F3Z0Srq2hCb64CSP7yQjM2MmCOcAVMsCbpb9LNYiPNMFAghqgKga3aU5CvQnJeF4EB0IPA5caBN3lKSKcPl7Nd1rfsxD-9v_bUmz9vUCvzobFKNm5HP698fSrk4AhqldYnP-RFRX3FL11FT6VmQkrGpjU3S55TtsWEQVxvLWEa6NVnTTfuUNKfXHjktg9ELfQ6Bx9uhU3RrNjKCLS-bd3KFPwbOzh6yXvoMNIjJp_Iqpm7HD-RyIa1Tyh1nTBxF8Vtt3wMnYCyp8W6kN8OMo_SjKMzcQcCJtEwM3vJLRMfOynlgYae7B_skWCMK2God9QEskw1D3TzG0PW9TWU3VY9EzUfKauJZ-2CSitDk1Ggc5Du5MPc_SwGRHGPA8CbW3ddvs0hMaoQUozpIhzyprhJMdDNPE0L3UPufuPKzHj99zY-Dk-_1aZSt_DPtAoGJVfD0RoLJ-OKTb8_XtlQS3vx4mmeSwD0oXTXrj4LlKkRWIc6SEjXbrN4t9Qh-K2bVR4PYXixhB3VtALwTZ5yzvaWssczj6h1dlCtVrLMMQjg8qCdJ494A4c1hyF-O1WXZ2y68Lut2PSk1ljssscL_wBV5Z_ZDeetGuVqV-nmGyFvDXuTtwnGUsDf_nuZxNEDPkWuqb7CZVL-oCCFKgsaLGQ2zPqC19ZY5qVkqgcbji2guYvSi2htlC4D8NSLCWcAAtr1dGurTVOT-c4ihT_M3fC6PCwtEykOEGZv4tWY0nuoqNUPxGDeU7eyWJS3hqz2iCJ-TAx32U1QuB8uAZ5q3YeplMprjDzsi-kkkl200H9Bgcah8_G43_oBKoioWPgA7kN33vhH4CywXMy_4ETgvpe_6aU9FViLG3b6pnYUP2oZ7LEP-ZG7k38sK0LH6adf7DjArx6Ccf5moc1tKClLR4HdVolB5OjQcyP7Le61ZVVK5e_AywHihTtF7v32jWUR9aZzrI7FKf7K4v-y490sTGJadd_xQylmg3uPMjJAbsiCjhLOZ4gEuZVCZ_VZD3chxTnAdx1PbvKHssJomkvAelW3bmCcc74yJyq8iYgST-aQ_cFcrtjAbKBl09UXV8AhYiYi4Uz-avFjqcnbNeNRsY6TeMQjUxRu7_hW8fiazXzjoyjR-VvSthpmJ4LwM6A-jfUWheiFiCkr47vOAaFJ850k7CwY0swREsyzFBDvQ7vXAXO6yMxxd7uzxqDDbL-Glyhz7pwfFoD3t_XGNS0UIr3O4VqxlP__31Xdco72y28UlMtb0HLt6lC2u_szOl2uccbwjoP1yWJKIuaBKuPgj4eYkDFaIYYI3D-JA-DUFhs02M-1ReS9vKBYkeGGlILGuCq2PBdaCxAkcBebVE5ek15MR4PE9qwH39gP7Zo1Ca48LzO9jHXBGWG_10hm_yKMWXOV4rbf_EbM8oV2a7EgfmYiZxCcQ8wpj55RGyJ9X2Qsf8fLbVFSL0_wtU-vmCR6IRtP77LaMCuPQEiv26QS4QCUWQQHKc38l_SYn8CGNk82bOIAzwukqLcaOuAbgFxWUWZpGEv5soJN2j1WxggVpLPxCr2WDyMeyn4zPNPfr1LoAccV1WF2Z1LGHGn4KeJBXqeAQF7hDlsiwPh8tLhuE3t818aBW6zXzBgjfxQEvRoGv0MohrKWmfEQOe9gudHXqL7P9fo9kOWmyhIfXWxIvmr5jiXbe8OQ-OFIMQ5b-k6BMm7Nzvq4DDIJ1Z-dOtjHr_EghhnR0xG6PvPzJCItGMRU5QD1RpHUfucvTquBdBq65b2f6uNH5OkC0dC-TjbxdXZ6aIf6gYkwJH5eOtAOn0v6OaElflf7bmLseEM1sF13TRarTvEG7l1WuuUr1EUR8Zk2_ESyX2RDsqJU_z_dMyMnVXE-7lZ75wsPHzUA4U-_YPbFsNw-zK9wSyLr6fr9qXrYmzD__4d53Zfu58f0UcoAhiQ_RyV0i7SFvkrYG07AlFqJfemQS7LE0ZOuIVPIKKFhd7ugtqeSle5tfcwNfmhwFuRmx8-Rs0KsJ6F62C8hgjYGzViQ8K_MM_Om5XAO-r-PZqyhB35wPTltgXsm294pfm4XU4xLSciERJUAmgsd78MydzqEqsC4SeY0SGr0Isw4lbe2Y385S69JAgOH-cSe4bGpoAU2pkkALitt84ObmgvO1cbUz_CwifpJoJVjxrSUVtgVMtWMlKxyk0TUxcdSbPAM55kQrOBRbevGVsDlw9LGqBf6wwUDmqfuQ-Alk5BtwIEFbu4HhIhuJpUqEZDLH_EukjGzUx5KD3GgZpoRCVk5O-muMxw203fj_upm2C6mAquVQTpmEbShQx2snOLmmRcXCVcMZ0CkcHnm8yixz6IerZMm-GqwUED3bT6Y4FLZtXqq5XxHQvegqccnnVfzzu-ejxZ88dQdnV_hcX5gPur-6sFv-VQIdyxwgVYomqnQTpmFVtvjS9ClDwhQHcTntkNHKoCYj_8vHfKq5FO6QgzW_PsBRIhIwkFOhxHTG-mENFkjy3rNRX1vT7fFEZA1LZl-QX0N793g3ovdwdUjC2ZpHKXrkBkxuz3kzY3VtLZq_cA9UBKrvoLBiSM2Nl34q2OCwNG8SAmMW4BKPyaH6TwQYApMVTIq87vIkme-mSmZ6T_qs4LGHg-vJ22QzjpUOy_IrBrq4XoYvInzvavOy6J5qOeefjPMpLzYkXpt0LE4nntsPtME0cROOP5JrdEHnK08LiImYGADyNW0fbVIxR-gMQcLfAaoewcEXkDeQVVyPsiSr7kzrFVbULpt7ktFSDao4x0kyxjhZK7IJMHWtpC2GN63NfbcOczy0gXe3pijC0Hlgvr1TFlT1JhMKcvX34S5X7o9Z6UdM8PEFhJwctg_WLpaQ-zJ7_LUI1RC7V3-JeARAoXW-9PylaOrZNmdZF1uR9m5CKtztZrdJ3ouTxZbA_uKYW-4i7RX1hXIViTJpmuvi4FAdV9hNPO2wJY0qc1AnDXVuNYYzkssnplX8X3w7oy6OnP4Pwp6SrC-vCWorvV1b0iniz0BTL0cFao_7KDkg-ez3cf_Km_049T-K5r2tBGn48ADplAgq8ZCaq2c2D7vMDdlYjzdYI61iUqGoQa841r7RtupmxVuu1akZ385Ss9j8RTj5g_UuSPBdwY7fV6OORROJhXuLkY5TfbN8uh1_Re7brqIllxA6GBJY69ZEDqKsYWPtE31vBOPsn42X7s8UMj32o4XkZCVM7VoXbA-7FJIxqkZxm6e08Huk7dFyTnuiCRrZ1KokyYTrlpkg_7AHxwWDEcjvTePnXgcqGdyeqh3flDRuQogVZReR4_N-o7DbIjcrywjCDYU3MjRz7im9qczaT42bVGvwtATf5QqutLPMzGIYdfhmfPA6dnCohsSUEYxW7m9DEizk1fYDmDnzGh8vj1Ab3qZ8JglSGbaxzvnO3UbM86r1aMZXFTfyHpEQMA93GCvOpL2-WkpZstD0j6JRrTGjTIPk2fktjLjYBrvoylXqfmm6o8myWnkh5h4vyJbsf51QnDTbY52RoH8ETdKXb_2q9-Med_wP1VHZNJ2eRwsbg9UyCt4yWsHJ3yfx_Za1bauh2GiTv8M3w0FFf4HqNz7smN3WWjK1g7bG2zW4pQ5-fip5vsU-jGz2MRRAu6LLGBc2-UbfsDZyQGlGWCoXoenypciQZVayPZyficK82A6TqtaBfHBJe08MCFFwDlLwwjZ7RHtUg506I4bDUro8BIgCvVbAL";
    }
}

