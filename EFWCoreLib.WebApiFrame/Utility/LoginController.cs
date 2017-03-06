﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using EFWCoreLib.CoreFrame.Business.AttributeInfo;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using EFWCoreLib.CoreFrame.SSO;
using EFWCoreLib.WcfFrame.DataSerialize;
using EFWCoreLib.WebFrame.WebAPI;
using EFWCoreLib.CoreFrame.Mongodb;
using EFWCoreLib.WcfFrame.Utility.MonitorPlatform;
using EFWCoreLib.WebApiFrame;
using EFWCoreLib.CoreFrame.ProcessManage;

namespace EFWCoreLib.WebAPI.Utility
{
    /// <summary>
    /// WebApi 登陆验证，然后调用wcf服务
    /// /efwplusApi/coresys/login/userlogin
    /// {"usercode":"admin","password":"1","wcfpluginname":"MainFrame.Service","wcfcontroller":"LoginController","wcfmethod":"UserLogin"}
    /// </summary>
    [efwplusApiController(PluginName = "coresys")]
    public class LoginController : WebApiController
    {
        [HttpPost]
        public LoginResponse UserLogin([FromBody] LoginRequest loginreq)
        {
            LoginResponse loginres = new LoginResponse();
            try
            {
                Action<ClientRequestData> requestAction = ((ClientRequestData request) =>
                {
                    request.Iscompressjson = false;
                    request.Isencryptionjson = false;
                    request.Serializetype = SerializeType.Newtonsoft;
                    request.AddData(loginreq.usercode);
                    request.AddData(loginreq.password);
                });

                ServiceResponseData response = InvokeWcfService(loginreq.wcfpluginname, loginreq.wcfcontroller, loginreq.wcfmethod, requestAction);
                loginres.otherdata = JsonConvert.DeserializeObject(response.GetJsonData());

                //AuthResult authres= SsoHelper.ValidateUserId(loginreq.usercode);
                //if (authres.ErrorMsg == string.Empty)
                //{
                //    loginres.flag = true;
                //    loginres.msg = string.Empty;
                //    loginres.token = authres.token;
                //    loginres.usercode = authres.User.UserId;
                //    loginres.username = authres.User.UserName;
                //    loginres.deptname = authres.User.DeptName;
                //    loginres.workname = authres.User.WorkName;
                //}
                //else
                //    throw new Exception(authres.ErrorMsg);
            }
            catch (Exception e)
            {
                loginres.flag = false;
                loginres.msg = "登录失败：" + e.Message;
            }
            return loginres;
        }
        //提交登录
        [HttpPost]
        public object submit([FromBody] simpleUser siuser)
        {
            MongoHelper<User> helperUser = new MongoHelper<User>(WebApiGlobal.MongoConnStr, MonitorPlatformManage.dbName);
            User user = helperUser.Find(MongoDB.Driver.Builders.Query.EQ("usercode", new MongoDB.Bson.BsonString(siuser.usercode)));
            if (user != null)
            {
                string pwd = CoreFrame.Common.DESEncryptor.DesEncrypt(siuser.password);
                if (user.pwd == pwd)
                {
                    string token = WebApiGlobal.normalIPC.CallCmd(IPCName.GetProcessName(IPCType.efwplusBase), "ssosignin", "usercode=" + user.usercode + "&username=" + user.username);
                    return new { flag = true, token = token };
                }
            }
            return new { flag = false, token = "" };
        }
        //验证token
        [HttpGet]
        public object validatetoken(string token)
        {
            string ret = WebApiGlobal.normalIPC.CallCmd(IPCName.GetProcessName(IPCType.efwplusBase), "ssovalidatetoken", "token=" + token);
            AuthResult ar = JsonConvert.DeserializeObject<AuthResult>(ret);
            if (string.IsNullOrEmpty(ar.ErrorMsg))
                return new { flag = true, username = ar.User.EmpName };
            return new { flag = false, username = "" };
        }
    }

    /// <summary>
    /// 登录请求
    /// </summary>
    public class LoginRequest
    {
        private string _usercode;
        /// <summary>
        /// 用户名
        /// </summary>
        public string usercode
        {
            get { return _usercode; }
            set { _usercode = value; }
        }
        
        private string _password;
        /// <summary>
        /// 密码
        /// </summary>
        public string password
        {
            get { return _password; }
            set { _password = value; }
        }
        /// <summary>
        /// 服务插件
        /// </summary>
        public string wcfpluginname { get; set; }
        /// <summary>
        /// 控制器
        /// </summary>
        public string wcfcontroller { get; set; }
        /// <summary>
        /// 方法名
        /// </summary>
        public string wcfmethod { get; set; }
    }
    /// <summary>
    /// 登录结果
    /// </summary>
    public class LoginResponse
    {
        public bool flag { get; set; }
        public string msg { get; set; }
        public string token { get; set; }
        //下面是用户信息
        public string usercode { get; set; }
        public string username { get; set; }
        public string deptname { get; set; }
        public string workname { get; set; }
        //登录返回的其他数据
        public Object otherdata { get; set; }
    }

    public class simpleUser
    {
        private string _usercode;
        /// <summary>
        /// 用户名
        /// </summary>
        public string usercode
        {
            get { return _usercode; }
            set { _usercode = value; }
        }

        private string _password;
        /// <summary>
        /// 密码
        /// </summary>
        public string password
        {
            get { return _password; }
            set { _password = value; }
        }
    }
}
