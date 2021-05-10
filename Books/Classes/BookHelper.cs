using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using HAP = HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace BookHelperLib
{
    public class BookHelper
    {
        #region 定义变量
        private static string LogsPath = Environment.CurrentDirectory + "\\Logs.txt";
        private static netProxy Proxysets = new netProxy();
        private static string cookie = "";
        private static string CoverSavePath = Path.GetTempPath();
        private static List<KeyValuePair<string, string>> SourceList = new List<KeyValuePair<string, string>>();
        private static List<RootSource> SourcePool = new List<RootSource>();
        private static List<Thread> ThreadPool = new List<Thread>();
        private static List<Book> BooksList = new List<Book>();
        #endregion

        #region 定义类

        public class netProxy
        {
            public bool Enable;
            public string Type;
            public string Host;
            public int Port;
            public string Username;
            public string Password;
        }
        public class RootSource
        {
            /// <summary>
            /// Source File Name
            /// </summary>
            public string RootName;       
            public List<bookSource> Source;
        }
        public struct Book
        {
            public string Name;
            public string Author;
            public string Coverurl;
            public string Coverbase64;
            public string Coverpath;
            public string Des;
            public string Url;
            public string Sourcename;
            public string RootSourcename;
            public int Read;
        }
        public class bookSource
        {
            public string sourceName;
            public string sourceUrl;
            public string sorts;
            public ruleSort ruleSort;
            public ruleBook ruleBook;
            public ruleChapter ruleChapter;
            public ruleContentTxt ruleContentTxt;
            public ruleSearch ruleSearch;
        }
        public class ruleSort
        {
            public string booklist;
            public string books;
            public string page;          
            
        }
        public class ruleBook
        {
            public string author;
            public string bookimg;
            public string des;          
            public string url;
        }
        public class ruleChapter
        {
            public string chapter;
        }
        public class ruleContentTxt
        {
            public string content;
            public string nextpage;
        }
        public class ruleSearch
        {
            public string url;
            public string method;
            public string searchkey;
            public string booklist;
            public string bookurl;
            public string page;
        }
        public class JsonUtil
        {
            /// <summary>
            /// 用于将Json格式的字符串反序化为List。 当传入的Json字符串有误的时候, 抛出一个异常（JsonException）
            /// </summary>
            /// <typeparam name="T">泛型</typeparam>
            /// <param name="jsonStr">Json字符串</param>
            /// <returns>List对象或者null</returns>
            public static List<T> JsonToObjList<T>(string jsonStr)
            {
                List<T> objList = null;
                try
                {
                    objList = JsonConvert.DeserializeObject<List<T>>(jsonStr);
                }
                catch (Exception e)
                {
                    throw new JsonException("Json的格式可能错误," + e.Message);
                }
                return objList;
            }
        }
        private static bookSource GetSource(string rootname, string sourcename)
        {
            bookSource BS = new bookSource();
            for (int i = 0; i < SourcePool.Count; i++)
            {
                if (SourcePool[i].RootName == rootname)
                {
                    List<bookSource> Nbs = SourcePool[i].Source;
                    for (int m = 0; m < Nbs.Count; m++)
                    {
                        if (Nbs[m].sourceName == sourcename)
                        {
                            BS = Nbs[m];
                            break;
                        }
                    }
                }

            }
            return BS;
        }
        #endregion

        #region 获取作品分类
        public static List<KeyValuePair<string, string>> GetBookSorts(string rootname, string sourcename)
        {
            bookSource BS = GetSource(rootname, sourcename);
            Uri mainurl = new Uri(BS.sourceUrl);
            string maindocstr = GetRequst(mainurl);
            if (string.IsNullOrWhiteSpace(maindocstr))
            {
                Logsadd("Failed to get website content, please check network.");
                return null;
            }
            HAP.HtmlDocument maindoc = new HAP.HtmlDocument();
            maindoc.LoadHtml(maindocstr);
            if (maindoc == null) return null;
            List<KeyValuePair<string, string>> sort = new List<KeyValuePair<string, string>>();
            string[] exp = BS.sorts.Split('|');           
            for (int m = 0; m < exp.Length; m++)
            {
                HAP.HtmlNodeCollection woods = maindoc.DocumentNode.SelectNodes(exp[m]);
                for (int i = 0; i < woods.Count; i++)
                {
                    string wood = woods[i].InnerText;
                    string woodurl = woods[i].Attributes["href"].Value;
                    sort.Add(new KeyValuePair<string, string>(wood, woodurl));
                }
            }
            return sort;
        }
        #endregion

        #region 获取分类书籍

        public static List<Book> GetBooksList(string rootname,string sourcename, string sorturl, out List<KeyValuePair<string, string>> PList)
        {
            ThreadPool.Clear();
            BooksList.Clear();

            bookSource BS = GetSource(rootname, sourcename);
            Uri Url = new Uri(BS.sourceUrl);
            string Listurl = sorturl;
        
            if (Regex.Match(Listurl, "^//").Length > 0)
            {
                Listurl = Url.Scheme + ":" + Listurl;
            }
            else if (Regex.Match(Listurl, "^/").Length > 0)
            {
                Listurl = Url.Scheme + "://" + Url.Host + Listurl;
            }

            string reurlstr = GetRequst(Listurl);
            if (string.IsNullOrWhiteSpace(reurlstr))
            {
                throw new Exception("Failed to get website content, please check network.");
            }
            HAP.HtmlDocument doc = new HAP.HtmlDocument();
            doc.LoadHtml(reurlstr);
            HAP.HtmlNodeCollection HC = doc.DocumentNode.SelectNodes(BS.ruleSort.booklist);
            HAP.HtmlNodeCollection Pages = doc.DocumentNode.SelectNodes(BS.ruleSort.page);
            List<Book> books = new List<Book>();

            for (int i = 0; i < HC.Count; i++)
            {
                HAP.HtmlDocument HD = new HAP.HtmlDocument();
                HD.LoadHtml(HC[i].InnerHtml);
                HAP.HtmlNode bookinfo = HD.DocumentNode.SelectSingleNode(BS.ruleSort.books);
                if (bookinfo == null) continue;
                string bname = bookinfo.InnerText;
                string burl = bookinfo.Attributes["href"].Value;
                if (Regex.Match(burl, "^//").Length>0)
                {
                    burl = Url.Scheme + ":" + burl;
                }else if (Regex.Match(burl, "^/").Length >0)
                {
                    burl = Url.Scheme + "://" + Url.Host + burl;
                }
                Book book = new Book();
                book.RootSourcename = rootname;
                book.Sourcename = sourcename;
                book.Name = bname;
                book.Url = burl;
                Thread infoThread = new Thread(() => GetBookInfo(book));
                ThreadPool.Add(infoThread);
                infoThread.Start();
                Thread.Sleep(200);
                //GetBookInfo(book);
            }

            while (true)
            {
                bool threadstatus = true;
                for(int i=0;i< ThreadPool.Count; i++)
                {
                    if (ThreadPool[i].IsAlive)
                    {
                        threadstatus = false;
                        break;
                    }
                }
                if (threadstatus)
                {
                    break;
                }else
                {
                    Thread.Sleep(200);
                }
            }

            List<KeyValuePair<string, string>> PagesList = new List<KeyValuePair<string, string>>();
            if (Pages != null)
            {
                for (int m = 0; m < Pages.Count; m++)
                {
                    string page = Pages[m].InnerText;
                    string pageurl = Pages[m].Attributes["href"].Value;
                    PagesList.Add(new KeyValuePair<string, string>(page, pageurl));
                }
            }
            PList = PagesList;
            return BooksList;
        }
        #endregion

        #region 获取搜索书信息
        public static List<Book> GetSearchBooksList(string keyvalue)
        {
            ThreadPool.Clear();
            BooksList.Clear();
            
            for (int i = 0; i < SourcePool.Count; i++)
            {
                Book book = new Book();
                for (int m = 0; m < SourcePool[i].Source.Count; m++)
                {
                    book.RootSourcename = SourcePool[i].RootName;
                    book.Sourcename = SourcePool[i].Source[m].sourceName;
                    bookSource BS = GetSource(book.RootSourcename, book.Sourcename);
                    string searchUrl = BS.ruleSearch.url;
                    Uri Requrl = new Uri(searchUrl);
                    string Method = BS.ruleSearch.method.ToUpper();
                    List<KeyValuePair<string, string>> param = new List<KeyValuePair<string, string>>();
                    param.Add(new KeyValuePair<string, string>(BS.ruleSearch.searchkey, keyvalue));

                    string responseUrl = "";
                    string result = DataRequst(Requrl, Method, param, out responseUrl);

                    if (string.IsNullOrWhiteSpace(result))
                    {
                        continue;
                    }
                    HAP.HtmlDocument HD = new HAP.HtmlDocument();
                    HD.LoadHtml(result);
                    HAP.HtmlNodeCollection Searchinfo = HD.DocumentNode.SelectNodes(BS.ruleSearch.booklist);
                    
                    if (Searchinfo == null) { continue; }
                    for (int n = 0; n < Searchinfo.Count; n++)
                    {
                        HAP.HtmlDocument WoodHD = new HAP.HtmlDocument();
                        WoodHD.LoadHtml(Searchinfo[n].InnerHtml);
                        HAP.HtmlNode bookinfo = WoodHD.DocumentNode.SelectSingleNode(BS.ruleSearch.bookurl);
                        if (bookinfo == null) { continue; }
                        string bookurl = bookinfo.Attributes["href"].Value;
                        if (Regex.Match(bookurl, "^//").Length > 0)
                        {
                            bookurl = Requrl.Scheme + ":" + bookurl;
                        }
                        else if (Regex.Match(bookurl, "^/").Length >0)
                        {
                            bookurl = Requrl.Scheme + "://" + Requrl.Host + bookurl;
                        }
                        book.Name = bookinfo.InnerText;
                        book.Url = bookurl;
                        Thread infoThread = new Thread(() => GetBookInfo(book));
                        ThreadPool.Add(infoThread);
                        infoThread.Start();
                        Thread.Sleep(200);
                    }                  
                }
            }
            while (true)
            {
                bool threadstatus = true;
                for (int x = 0; x < ThreadPool.Count; x++)
                {
                    if (ThreadPool[x].IsAlive)
                    {
                        threadstatus = false;
                        break;
                    }
                }
                if (threadstatus)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(200);
                }
            }
            return BooksList;
        }
        #endregion

        #region 获取书信息
        public static void GetBookInfo(Book book)
        {
            Uri url = new Uri(book.Url);
            string reurlstr = GetRequst(url);
            if (string.IsNullOrWhiteSpace(reurlstr))
            {
                Logsadd("Failed to get website content, please check network.");
                return;
            }
            HAP.HtmlDocument bookinfo = new HAP.HtmlDocument();
            bookinfo.LoadHtml(reurlstr);
            bookSource BS = GetSource(book.RootSourcename, book.Sourcename);
            string imgurl = bookinfo.DocumentNode.SelectSingleNode(BS.ruleBook.bookimg).Attributes["src"].Value;

            if (Regex.Match(imgurl, "^//").Length > 0)
            {
                imgurl = url.Scheme + ":" + imgurl;
            }
            else if (Regex.Match(imgurl, "^/").Length >0)
            {
                imgurl = url.Scheme + "://" + url.Host + imgurl;
            }

            book.Coverurl = imgurl;
            book.Coverpath = CoverSavePath + book.Name + Path.GetExtension(book.Coverurl);
            book.Des = bookinfo.DocumentNode.SelectSingleNode(BS.ruleBook.des).InnerText;
            book.Author = bookinfo.DocumentNode.SelectSingleNode(BS.ruleBook.author).InnerText;      
            BooksList.Add(book);
        }
        #endregion

        #region 获取书籍章节
        public static List<KeyValuePair<string, string>> GetBookContents(Book book)
        {
            Uri Url = new Uri(book.Url);
            string docstr = GetRequst(Url);
            if (string.IsNullOrWhiteSpace(docstr))
            {
                Logsadd("Failed to get website content, please check network.");
                return null;
            }
            HAP.HtmlDocument doc = new HAP.HtmlDocument();
            doc.LoadHtml(docstr);
            bookSource BSource = GetSource(book.RootSourcename, book.Sourcename);
            string Txturl = "";
            if (BSource.ruleBook.url == "nowpage")
            {
                Txturl = book.Url;
            }
            else
            {
                Txturl = doc.DocumentNode.SelectSingleNode(BSource.ruleBook.url).Attributes["href"].Value;
            }
            

            if (Regex.Match(Txturl, "^//").Length > 0)
            {
                Txturl = Url.Scheme + ":" + Txturl;
            }
            else if (Regex.Match(Txturl, "^/").Length >0)
            {
                Txturl = Url.Scheme + "://" + Url.Host + Txturl;
            }

            //string Getwood = Url.Scheme + "://" + Url.Host + Txturl;
            HAP.HtmlDocument wooddoc = new HAP.HtmlDocument();
            wooddoc.LoadHtml(GetRequst(Txturl));
            HAP.HtmlNodeCollection wood = wooddoc.DocumentNode.SelectNodes(BSource.ruleChapter.chapter);
            List<KeyValuePair<string, string>> Contents = new List<KeyValuePair<string, string>>();
            if (wood == null) return Contents;
            for (int i = 0; i < wood.Count; i++)
            {
                string tit = wood[i].InnerText;
                string titurl = wood[i].Attributes["href"].Value;
                if (Regex.Match(titurl, "^//").Length > 0)
                {
                    titurl = Url.Scheme + ":" + titurl;
                }
                else if (Regex.Match(titurl, "^/").Length >0)
                {
                    titurl = Url.Scheme + "://" + Url.Host + titurl;
                }
                Contents.Add(new KeyValuePair<string, string>(tit, titurl));
            }
            return Contents;         
        }
        #endregion

        #region 获取章节内容
        public static string GetContentTxt(string url,Book book)
        {
            string Txt = null;
            string res =GetRequst(url);
            if (string.IsNullOrWhiteSpace(res))
            {
                Logsadd("Failed to get website content, please check network.");
                return null;
            }
            HAP.HtmlDocument doc = new HAP.HtmlDocument();
            doc.LoadHtml(res);
            bookSource BSource =GetSource(book.RootSourcename, book.Sourcename);
            Txt = doc.DocumentNode.SelectSingleNode(BSource.ruleContentTxt.content).InnerText;

            if (!string.IsNullOrWhiteSpace(BSource.ruleContentTxt.nextpage))
            {
                HAP.HtmlNode HN = doc.DocumentNode.SelectSingleNode(BSource.ruleContentTxt.nextpage);
                if (HN!=null && HN.InnerText.Trim() == "下一页")
                {
                    string nexturl = doc.DocumentNode.SelectSingleNode(BSource.ruleContentTxt.nextpage).Attributes["href"].Value;
                    if (!string.IsNullOrWhiteSpace(nexturl))
                        Txt += GetContentTxt(GetAllUrl(nexturl, new Uri(url)), book);
                }       
            }

            Txt.Replace("<br/>", Environment.NewLine);
            Txt.Replace("　　", "");
            return Txt;
        }
        #endregion

        #region 设置封面图片保存位置
        /// <summary>
        /// 设置封面保存位置
        /// </summary>
        /// <param name="path">要保存的文件夹</param>
        public static void SetCovePath(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            CoverSavePath = path;
        }
        #endregion

        #region 读取图片
        /// <summary>
        /// 通过FileStream 来打开文件
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Bitmap ReadImageFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;//文件不存在
            }
            FileStream fs = File.OpenRead(filename); //OpenRead
            int filelength = 0;
            filelength = (int)fs.Length; //获得文件长度 
            Byte[] image = new Byte[filelength]; //建立一个字节数组 
            fs.Read(image, 0, filelength); //按字节流读取 
            Image result = Image.FromStream(fs);
            fs.Close();
            Bitmap bit = new Bitmap(result);
            return bit;
        }
        #endregion

        #region 设置、获取、取消代理
        /// <summary>
        /// 获取代理参数
        /// </summary>
        public static netProxy GetProxy()
        {
            return Proxysets;
        }
        /// <summary>
        /// 设置代理
        /// </summary>
        /// <param name="Host">代理主机</param>
        /// <param name="Port">代理端口</param>
        /// <param name="Username">用户名</param>
        /// <param name="Password">密码</param>
        public static void SetProxy(bool Enable,string Type,string Host,int Port,string Username,string Password)
        {
            Proxysets.Enable = Enable;
            Proxysets.Type = Type;
            Proxysets.Host = Host;
            Proxysets.Port = Port;
            Proxysets.Username = Username;
            Proxysets.Password = Password;
        }
        #endregion

        #region 获取、添加、删除源地址
        /// <summary>
        /// 获取源地址
        /// </summary>
        /// <returns></returns>
        public static List<KeyValuePair<string,string>> GetSoucerAdress()
        {
            return SourceList;
        }
        /// <summary>
        /// 添加源地址
        /// </summary>
        /// <param name="name">源名称</param>
        /// <param name="address">源地址</param>
        /// <param name="content">内容</param>
        public static void AddSoucerAdress(string name,string address,string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                content = GetRequst(address);
            }

            SourceList.Add(new KeyValuePair<string, string>(name, address));
            RootSource RS = new RootSource();
            RS.Source = JsonUtil.JsonToObjList<bookSource>(content);
            RS.RootName = name;
            SourcePool.Add(RS);
        }

        /// <summary>
        /// 删除源地址
        /// </summary>
        /// <param name="rootsourcename">源名称</param>
        public static void DelSourceAdress(string rootsourcename)
        {
           for(int i=0;i< SourceList.Count; i++)
            {
                KeyValuePair<string, string> source = SourceList[i];
                if (source.Key == rootsourcename)
                {
                    SourceList.Remove(source);
                    break;
                }
            }

            for (int m = 0; m < SourcePool.Count; m++)
            {
                if (SourcePool[m].RootName == rootsourcename)
                {
                    SourcePool.Remove(SourcePool[m]);
                    break;
                }
            }
        }
        /// <summary>
        /// 清除源
        /// </summary>
        public static void CleanSource()
        {
            SourcePool.Clear();
        }

        #endregion

        #region 获取网页内容

        private static HttpClient GetHttpClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClientHandler handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
            if (Proxysets.Enable)
            {
                WebProxy Tproxy = new WebProxy(Proxysets.Host, Proxysets.Port);
                if (!string.IsNullOrWhiteSpace(Proxysets.Username) && !string.IsNullOrWhiteSpace(Proxysets.Password))
                {
                    NetworkCredential nc = new NetworkCredential(Proxysets.Username, Proxysets.Password);
                    Tproxy.Credentials = nc;
                }
                handler.Proxy = Tproxy;
            }
            HttpClient httpClient = new HttpClient(handler);
            httpClient.Timeout = new TimeSpan(0, 0, 20);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            //httpClient.DefaultRequestHeaders.Add("ContentType", "text/html;charset=utf-8");
            //httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
            return httpClient;
        }

        /// <summary>
        /// 返回完整网站链接
        /// </summary>
        /// <param name="Testurl">测试链接</param>
        /// <param name="Url">主页地址</param>
        /// <returns></returns>
        private static string GetAllUrl(string Testurl,Uri Url)
        {
            string NewUrl = null;
            if (Regex.Match(Testurl, "^//").Length > 0)
            {
                NewUrl = Url.Scheme + ":" + Testurl;
            }
            else if (Regex.Match(Testurl, "^/").Length > 0)
            {
                NewUrl = Url.Scheme + "://" + Url.Host + Testurl;
            }else
            {
                NewUrl = Url.ToString();
            }
            return NewUrl;
        }
        /// <summary>
        /// Get方式获取网页内容
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="n">重试次数</param>
        /// <returns>网页内容</returns>
        public static string GetRequst(string url, int n = 3)
        {
            if (File.Exists(url))
            {
                return File.ReadAllText(url);
            }
            return GetRequst(new Uri(url));
        }

        /// <summary>
        /// Get方式获取网页内容
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="n">重试次数</param>
        /// <returns>网页内容</returns>
        public static string GetRequst(Uri url,int n=3)
        {

            using (HttpClient Hclient = GetHttpClient())
            {
                //尝试3次，都失败，返回空
                for (int i = 0; i < n; i++)
                {
                    try
                    {
                        string result = Hclient.GetStringAsync(url).Result;

                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        Logsadd(e.Message);
                        continue;
                    }
                }
                return "";
            }
        }
        
        public static string DataRequst(string url,string Method, List<KeyValuePair<string, string>> param, out string responseUrl, string cookie = "")
        {
            return DataRequst(new Uri(url), Method, param, out responseUrl, cookie);
        }
        public static string DataRequst(Uri url, string Method, List<KeyValuePair<string, string>> param, out string responseUrl, string cookie = "")
        {
            if (Method.Trim().ToUpper() == "POST")
            {
                return PostRequst(url, param, out responseUrl, cookie);
            }
            else
            {
                string data = "?";
                for(int i = 0; i < param.Count; i++)
                {
                    if (i == 0) {
                        data += param[i].Key + "=" + param[i].Value;
                    }else
                    {
                        data += "&" + param[i].Key + "=" + param[i].Value;
                    }               
                }
                responseUrl = url.ToString();
                return GetRequst(url.ToString() + data);
            }
        }
        private static string PostRequst(Uri url, List<KeyValuePair<string, string>> param, out string responseUrl, string cookie = "")
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                using (HttpClient Hclient = GetHttpClient())
                {
                    HttpContent cont = new FormUrlEncodedContent(param);
                    cont.Headers.Add("HttpRequestHeader.ContentType", "application/x-www-form-urlencoded; charset=UTF-8");
                    //cont.Headers.Add("HttpRequestHeader.Cookie", cookie);               
                    System.Threading.Tasks.Task<HttpResponseMessage> responseMessage = Hclient.PostAsync(url, cont);
                    responseMessage.Wait();
                    System.Threading.Tasks.Task<string> reString = responseMessage.Result.Content.ReadAsStringAsync();
                    reString.Wait();
                    responseUrl = responseMessage.Result.RequestMessage.RequestUri.ToString();
                    return reString.Result;
                }              
            }
            catch (Exception e)
            {
                Logsadd(e.Message);
                responseUrl = null;
                return "";
            }           
        }

        #endregion

        #region 下载文件
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url">文件地址</param>
        /// <param name="filename">保存文件名</param>
        /// <param name="n">重试次数</param>
        public static void DownloadFile(string url,string filename,int n=3)
        {
            if (File.Exists(filename)) return;
            HttpClient client = GetHttpClient();
            //尝试3次
            for (int i = 0; i < n; i++)
            {
                try
                {
                    byte[] res = client.GetByteArrayAsync(url).Result;
                    File.WriteAllBytes(filename, res);
                    if (File.Exists(filename))
                    {
                        client.Dispose();
                        return;
                    }
                }
                catch (Exception e)
                {
                    if (File.Exists(filename))
                    {
                        File.Delete(filename);
                    }
                    Logsadd(e.Message);
                    continue;
                }
            }
        }
        #endregion

        #region 获取根源清单
        public static List<string> GetRootSourceName()
        {
            List<string> sources = new List<string>();
            for(int i=0;i< SourcePool.Count; i++)
            {
                sources.Add(SourcePool[i].RootName);
            }
            return sources;
        }
        #endregion

        #region 获取源清单
        public static List<string> GetSourceName(string rootname)
        {
            List<string> sources = new List<string>();
            for (int i = 0; i < SourcePool.Count; i++)
            {
                if (SourcePool[i].RootName == rootname)
                {
                    List<bookSource> Lbs = SourcePool[i].Source;
                    for(int m = 0; m < Lbs.Count; m++)
                    {
                        sources.Add(Lbs[m].sourceName);
                    }
                    return sources;
                }
            }
            return sources;
        }
        #endregion

        #region 记录异常信息日志
        /// <summary>
        /// 设置日志保存地址
        /// </summary>
        /// <param name="pathname">保存文件名(默认当前运行目录下)</param>
        public static void SetLogsPath(string pathname)
        {
            if (File.Exists(pathname))
            {
                LogsPath = pathname;
            }
        }
        /// <summary>
        /// 添加日志
        /// </summary>
        /// <param name="log">日志信息</param>
        public static void Logsadd(string log)
        {
            try
            {
                string str = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + ": " + log + Environment.NewLine;
                File.AppendAllText(LogsPath, str);
            }
            catch (Exception)
            {
                Thread.Sleep(100);
                Logsadd(log);
            }
        }
        #endregion

        #region 图片转换
        /// <summary>
        /// base64 转 Image
        /// </summary>
        /// <param name="base64">base64字符串</param>
        public static Bitmap Base64ToImage(string base64)
        {
            base64 = base64.Replace("data:image/png;base64,", "").Replace("data:image/jgp;base64,", "").Replace("data:image/jpg;base64,", "").Replace("data:image/jpeg;base64,", "");//将base64头部信息替换
            byte[] bytes = Convert.FromBase64String(base64);
            MemoryStream memStream = new MemoryStream(bytes);
            Image mImage = Image.FromStream(memStream);
            Bitmap bp = new Bitmap(mImage);
            return bp;
        }

        /// <summary>
        /// Image 转成 base64
        /// </summary>
        /// <param name="fileFullName">图片文件路径</param>
        public static string ImageToBase64(string fileFullName)
        {
            try
            {
                Bitmap bmp = new Bitmap(fileFullName);
                MemoryStream ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] arr = new byte[ms.Length]; ms.Position = 0;
                ms.Read(arr, 0, (int)ms.Length); ms.Close();
                return Convert.ToBase64String(arr);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
