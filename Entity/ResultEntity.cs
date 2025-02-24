using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity
{
    public class ResultEntity<T>
    {
        /// <summary>
        /// 返回状态码
        /// </summary>
        private int resultCode;
        public int ResultCode
        {
            get { return resultCode == 0 ? 200 : resultCode; }
            set { resultCode = value; }
        }
        /// <summary>
        /// 返回信息
        /// </summary>
        private string resultMsg { get; set; }
        public string ResultMsg
        {
            get { return string.IsNullOrEmpty(resultMsg) ? "success" : resultMsg; }
            set { resultMsg = value; }
        }
        /// <summary>
        /// 返回结果集
        /// </summary>
        public T ResultData { get; set; }
    }
}
