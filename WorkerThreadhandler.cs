using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;

namespace WebGISSocket
{
    public class CONSTANT
    {
        public static string ROOT = "D:\\课程内容\\大三上\\网络基础与WebGIS\\wms\\WebGIS-Server\\";
    }
    public class GISThreadHandler
    {
        public static MemoryStream ms; // 静态内存流，每次调用不需要重新读取shapefile

        public TcpListener myTcplistener;

        static string webRoot = CONSTANT.ROOT + "www\\";

        static Tuple<string, string> defaultPage = new Tuple<string, string>("text/xml", "WMS\\wms.xml");

        public void HandleThread()
        {
            try
            {
                Socket socket = myTcplistener.AcceptSocket();

                byte[] recv_Buffer = new byte[1024 * 640];
                int recv_Count = socket.Receive(recv_Buffer); //接收浏览器的请求数据
                string recv_request = Encoding.UTF8.GetString(recv_Buffer, 0, recv_Count);
                //Resolve(recv_request, web_client);  //解析、路由、处理
                Tuple<string, string> parseResult = RouteHandle(recv_request);
                string type = parseResult.Item1;
                string dir = parseResult.Item2;
                byte[] cont = pageHandle(type, dir);
                sendPageContent (cont, socket, type);
            }
            catch (Exception ex)
            {
                writeLog("Error!");
            }
        }

        // 发送HTTP报文
        static void sendPageContent(byte[] pageContent, Socket response, string type)
        {
            string statusline = "HTTP/1.1 200 OK\r\n"; //状态行
            byte[] statusline_to_bytes = Encoding.UTF8.GetBytes(statusline);
            byte[] content_to_bytes = pageContent;

            string header =
                string.Format("Content-Type:"+type+";charset=UTF-8\r\nContent-Length:{0}\r\n",
                    content_to_bytes.Length);
            byte[] header_to_bytes = Encoding.UTF8.GetBytes(header); //应答头

            response.Send (statusline_to_bytes); //发送状态行
            response.Send (header_to_bytes); //发送应答头
            response.Send(new byte[] { (byte) '\r', (byte) '\n' }); //发送空行
            response.Send (content_to_bytes); //发送正文（html）

            response.Close();
        }

        /// <summary>
        /// 解析URL，并做相应处理
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        static Tuple<string, string> RouteHandle(string request)
        {
            Tuple<string, string> retRoute = defaultPage;
            string[] strs = request.Split(new string[] { "\r\n" }, StringSplitOptions.None); //以“换行”作为切分标志

            if (strs.Length > 0) //解析出请求路径、post传递的参数(get方式传递参数直接从url中解析)
            {
                // 打印参数
                foreach (string str in strs)
                {
                    Console.WriteLine (str);
                }

                string[] items = strs[0].Split(' '); //items[1]表示请求url中的路径部分（不含主机部分）
                string pageName = items[1];
                string post_data = strs[strs.Length - 1]; //最后一项,用于处理POST

                //Dictionary<string, string> dict = ParameterHandle(strs);
                string real_data = MyUrlDeCode(post_data, Encoding.UTF8);
                if (real_data.Length > 0)
                {
                    Console.WriteLine (real_data);
                    string name = real_data.Split('=')[1];
                    Console.WriteLine (name);
                }

                string[] pages = pageName.Split('?');
                string type = pages[0].Split('/')[1].ToLower(); // WMS or WMTS
                Dictionary<string, string> get_param = new Dictionary<string, string>();
                
                if (pageName == "wmts\\1.0.0\\WMTSCapabilities.xml")
                {
                    return new Tuple<string, string>("text/xml", "WMTS\\wmts.xml");
                }

                if (pages.Length > 1)
                {
                    foreach (string str in pages[1].Split('&'))
                    {
                        if (str.Split('=').Length != 2)
                        {
                            continue;
                        }
                        string key = str.Split('=')[0].ToLower();
                        string value = str.Split('=')[1].ToLower();
                        Console.WriteLine(key + ',' + value);
                        get_param[key] = value;
                    }
                } 
                else
                {
                    if (type == "wms")
                    {
                        return new Tuple<string, string>("text/xml", "WMS\\wms.xml");

                    }
                    return new Tuple<string, string>("text/xml", "WMTS\\wmts.xml");
                }
                try
                {
                    if (get_param["request"].Equals("getcapabilities"))
                    {
                        if (type == "wms")
                        {
                            return new Tuple<string, string>("text/xml", "WMS\\wms.xml");
                        }
                        else if (type == "wmts")
                        {
                            return new Tuple<string, string>("text/xml", "WMTS\\wmts.xml");
                        }
                    }

                    else if (get_param["request"] == "getmap")
                    {
                        string[] bbox = get_param["bbox"].Split(',');
                        double y1 = Double.Parse(bbox[0]);
                        double y2 = Double.Parse(bbox[2]);
                        double x1 = Double.Parse(bbox[1]);
                        double x2 = Double.Parse(bbox[3]);
                        int width = Int32.Parse(get_param["width"]);
                        int height = Int32.Parse(get_param["height"]);

                        // TODO: 这里应当调用的获得文件路径的代码
                        ms = new MemoryStream();
                        ms = ShapeFileHelper.ReadShapeFile(width, height, x1, x2, y1, y2);
                        return new Tuple<string, string>("image/png", "Memory");
                    }

                    else if (get_param["request"] == "gettile")
                    {
                        int tilenum = Int32.Parse(get_param["tilematrix"].Split(':')[2]);
                        int row = Int32.Parse(get_param["tilerow"]);
                        int col = Int32.Parse(get_param["tilecol"]);


                        long real_row = (1L << tilenum) - row - 1;
                        int width = ((tilenum / 6) + 1) * 2;
                        string pngPath = "tiles" + "\\EPSG_4326_" + tilenum.ToString() + "\\" + String.Format("{0:D"+width.ToString()+ "}_{1:D" + width.ToString() + "}.png", col, real_row);
                        Console.WriteLine(pngPath);
                        if (System.IO.File.Exists(CONSTANT.ROOT + pngPath))
                            Console.WriteLine(pngPath);
                        else
                        {
                            pngPath = "tiles\\empty.png";
                        }
                                            
                                                // TODO: 这里应当调用某个文件地址
                        return new Tuple<string, string>("image/png", pngPath);

                    }
                }
                catch
                {
                    return defaultPage;
                }
                return defaultPage;
            }

            return retRoute;
        }

        // 编码URL
        public static string MyUrlDeCode(string str, Encoding encoding)
        {
            if (encoding == null)
            {
                Encoding utf8 = Encoding.UTF8;

                //首先用utf-8进行解码

                string code = HttpUtility.UrlDecode(str.ToUpper(), utf8);

                //将已经解码的字符再次进行编码.
                string encode = HttpUtility.UrlEncode(code, utf8).ToUpper();

                if (str == encode)
                    encoding = Encoding.UTF8;
                else
                    encoding = Encoding.GetEncoding("gb2312");
            }

            return HttpUtility.UrlDecode(str, encoding);
        }

        // 生成字节流
        static byte[] pageHandle(string type, string pagePath)
        {
            byte[] pageContent = null;
            
            if (type == "text/xml")
            {
                pagePath = pagePath.TrimEnd('/', '\\');
                if (System.IO.File.Exists(webRoot + pagePath))
                    pageContent = System.IO.File.ReadAllBytes(webRoot + pagePath);
                if (pageContent == null)
                {
                    string content = notExistPage();
                    pageContent = Encoding.UTF8.GetBytes(content);
                }
                return pageContent;
            }
            else 
            {
                if (type != "image/png")
                {
                    Console.WriteLine("Wrong!!!");
                }

                if(pagePath == "Memory")
                {
                    byte[] buffer = new byte[ms.Length];
                    //Image.Save()会改变MemoryStream的Position，需要重新Seek到Begin也就是开始的0位置
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.Read(buffer, 0, buffer.Length);

                    return buffer;
                }

                FileStream fs = new FileStream(CONSTANT.ROOT + pagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                BinaryReader reader = new BinaryReader(fs);
                byte[] imageData = new byte[fs.Length];
                int read;
                int imageTotBytes = 0;
                while ((read = reader.Read(imageData, 0, imageData.Length)) != 0)
                {
                    imageTotBytes = imageTotBytes + read;
                }
                reader.Close();
                fs.Close();

                return imageData;
            }
        }

        static void writeLog(string msg)
        {
            Console.WriteLine("  " + msg);
        }

        static string notExistPage()
        {
            string cont =
                @"<!DOCTYPE HTML>
<html>

    <head>
        <link rel='stylesheet' type='text/css' href='NewErrorPageTemplate.css' >

        <meta http-equiv='Content-Type' content='text/html; charset=UTF-8'>
        <title>This page can&rsquo;t be displayed</title>

        <script src='errorPageStrings.js' language='javascript' type='text/javascript'>
        </script>
        <script src='httpErrorPagesScripts.js' language='javascript' type='text/javascript'>
        </script>
    </head>

    <body onLoad='javascript:getInfo();'>
        <div id='contentContainer' class='mainContent'>
            <div id='mainTitle' class='title'>This page can&rsquo;t be displayed</div>
            <div class='taskSection' id='taskSection'>
                <ul id='cantDisplayTasks' class='tasks'>
                    <li id='task1-1'>Make sure the web address <span id='webpage' class='webpageURL'></span>is correct.</li>
                    <li id='task1-2'>Look for the page with your search engine.</li>
                    <li id='task1-3'>Refresh the page in a few minutes.</li>
                </ul>
                <ul id='notConnectedTasks' class='tasks' style='display:none'>
                    <li id='task2-1'>Check that all network cables are plugged in.</li>
                    <li id='task2-2'>Verify that airplane mode is turned off.</li>
                    <li id='task2-3'>Make sure your wireless switch is turned on.</li>
                    <li id='task2-4'>See if you can connect to mobile broadband.</li>
                    <li id='task2-5'>Restart your router.</li>
                </ul>
            </div>
            <div><button id='diagnose' class='diagnoseButton' onclick='javascript:diagnoseConnectionAndRefresh(); return false;'>Fix connection problems</button></div>
        </div>
    </body>
</html>";

            return cont;
        }
    }
}
