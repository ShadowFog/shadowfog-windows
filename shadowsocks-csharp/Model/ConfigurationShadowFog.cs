/**************File added by Ian.May, 2016 Sept. 26*************/
using System;
using System.IO;

using System.Text;
using System.Net;
using System.Windows.Forms;
using System.Text.RegularExpressions;

using Shadowsocks.Controller;

namespace Shadowsocks.Model
{
    [Serializable]
    public class ConfigurationShadowFog : Configuration
    {
        public int transactionID;
        public int errorcode;
        public int expires_in;
        public string errormsg;
        public string access_token;

/****************************************************************************************************************************/
/****************** The following part tries to request the scheduler for getting a fogNode as proxy*************************/
/****************************************************************************************************************************/

        public static string GetFogNodeList(ClientUser User)
        {
            string temp = GetFogScheduler();
            /******************************************************/
             Console.WriteLine("Scheduler Address: " + temp);
            /******************************************************/
            return GetFogCandidates(temp, User);
        }

        public static string GetFogScheduler()
        {
            string content;
            HttpWebRequest myHttpWebRequest = null;
            HttpWebResponse myHttpWebResponse = null;
            string[] url = new string[] { "https://git.oschina.net/ShadowFogNetwork/Information/raw/master/schedulerAddressTemp.txt",
                                          "https://raw.githubusercontent.com/ShadowFog/Information/master/schedulerAddressTemp.txt",
                                          "https://bitbucket.org/shadowfogteam/schedulerinfomation/raw/0cec08789ccbf8e38f927e52a267bc20309f0c62/schedulerAddressTemp.txt" };
            for (int url_cnt = 0; url_cnt < url.Length; url_cnt++)
            {
                if (null == myHttpWebResponse)
                {
                    myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url[url_cnt]);
                    myHttpWebRequest.AllowAutoRedirect = true; // be capable to handle 301/302/304/307 automatically
                    myHttpWebRequest.Referer = Regex.Match(url[url_cnt], "(?<=://).*?(?=/)").Value;
                    myHttpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.101 Safari/537.36";
                    try
                    {
                        myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                    }
                    catch (Exception e)
                    {
                        //continue;
                        MessageBox.Show(I18N.GetString("Fail to Get Scheduler address!"));
                        //Console.WriteLine(e.Message);
                    }
                }
                else break;
            }
            Stream receiveStream = myHttpWebResponse.GetResponseStream();
            Encoding encode = Encoding.GetEncoding("utf-8");
            StreamReader readStream = new StreamReader(receiveStream, encode);
            content = readStream.ReadToEnd();
            myHttpWebResponse.Close();
            readStream.Close();
            return content;
        }

        public static string GetFogCandidates(string SchedulerURL, ClientUser User)
        {
            //ClientUser's parameters are highly correlated with time, so these paras should be generate in real time;
            User.timeStamp = User.GetTimeStamp();
            User.nounce = User.GetNounce();
            User.transactionID = User.GetTransactionID();
            User.signature = User.GetSignature();

            string content;
            
            HttpWebRequest myHttpWebRequest = null;
            HttpWebResponse myHttpWebResponse = null;
            /***********************************************************************************************************************************************************************/
            //myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://192.168.31.203/scheduler.php" + "?username=" + User.name + "&timestamp=" + User.timeStamp + "&nonce=" + User.nounce + "&transactionID=" + User.transactionID + "&signature=" + User.signature);
            myHttpWebRequest = (HttpWebRequest)WebRequest.Create(SchedulerURL + "?username=" + User.name + "&timestamp=" + User.timeStamp + "&nonce=" + User.nounce + "&transactionID=" + User.transactionID + "&signature=" + User.signature);
            /***********************************************************************************************************************************************************************/

            Console.WriteLine("?username=" + User.name);
            Console.WriteLine("&timestamp=" + User.timeStamp);
            Console.WriteLine("&nonce=" + User.nounce);
            Console.WriteLine("&transactionID=" + User.transactionID);
            Console.WriteLine("&signature=" + User.signature);
            // handle bad http responses
            try
            {
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
            }
            catch (Exception e)
            {
                MessageBox.Show(I18N.GetString("No Response from Scheduler!"));
                return null;
            }
            // end handle bad http reponses
            Stream receiveStream = myHttpWebResponse.GetResponseStream();
            Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
            StreamReader readStream = new StreamReader(receiveStream, encode);
            content = readStream.ReadToEnd();
            myHttpWebResponse.Close();
            readStream.Close();
            return content;
        }
        
    }
}
