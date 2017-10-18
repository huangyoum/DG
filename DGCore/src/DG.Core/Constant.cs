﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DG.Core
{
    public class Constant
    {
        /// <summary>
        /// 错误代码
        /// 错误代码格式--前缀+xxxx
        /// </summary>
        public class ErrorCode
        {
            #region 系统异常代码--前缀S

            /// <summary>
            /// 系统异常
            /// </summary>
            public const string Exception = "S0001";

            /// <summary>
            /// 用户未登录
            /// </summary>
            public const string AuthorizeError = "S0002";

            /// <summary>
            /// Token过期
            /// </summary>
            public const string TokenOutTime = "S0003";

            /// <summary>
            /// 功能未实现
            /// </summary>
            public const string NotImplemented = "S0004";

            #endregion

            #region 公共错误代码--前缀C

            /// <summary>
            /// 参数为空
            /// </summary>
            public const string EmptyParameter = "C0001";

            #endregion
        }
    }
}