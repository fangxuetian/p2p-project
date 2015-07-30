﻿using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Lip2p.REST.Common;

namespace Lip2p.REST.Filters
{
    // 文档 http://stackoverflow.com/questions/33969/best-way-to-implement-request-throttling-in-asp-net-mvc
    /// <summary>
    /// Decorates any MVC route that needs to have client requests limited by time.
    /// </summary>
    /// <remarks>
    /// Uses the current System.Runtime.Caching.MemoryCache to store each client request to the decorated route.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ThrottleAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// A unique name for this Throttle.
        /// </summary>
        /// <remarks>
        /// We'll be inserting a Cache record based on this name and client IP, e.g. "Name-192.168.0.1"
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// The number of seconds clients must wait before executing this decorated route again.
        /// </summary>
        public int Seconds { get; set; }

        /// <summary>
        /// A text message that will be sent to the client upon throttling.  You can include the token {n} to
        /// show this.Seconds in the message, e.g. "Wait {n} seconds before trying again".
        /// </summary>
        public string Message { get; set; }

        public bool ThrottleSuccessRequestOnly { get; set; }

        protected virtual string KeyParamGenerater(HttpActionContext actionContext)
        {
            return actionContext.Request.GetClientIp();
        }

        private string GenerateKey(HttpActionContext actionContext)
        {
            return string.Concat(Name, "-", KeyParamGenerater(actionContext));
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var key = GenerateKey(actionContext);

            if (MemoryCache.Default[key] == null) return; // not access before

            if (string.IsNullOrEmpty(Message))
            {
                Message = "You may only perform this action every {n} seconds.";
            }

            var httpResponseMessage = actionContext.Request.CreateResponse(
                (HttpStatusCode) 429, // 429 Too Many Requests
                Message.Replace("{n}", Seconds.ToString()));
            httpResponseMessage.ReasonPhrase = "Too Many Requests";
            actionContext.Response = httpResponseMessage;
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (ThrottleSuccessRequestOnly)
            {
                if (actionExecutedContext.Response.IsSuccessStatusCode)
                {
                    var key = GenerateKey(actionExecutedContext.ActionContext);
                    MemoryCache.Default.Set(key, true, DateTime.Now.AddSeconds(Seconds));
                }
            }
            else
            {
                var key = GenerateKey(actionExecutedContext.ActionContext);
                MemoryCache.Default.Set(key, true, DateTime.Now.AddSeconds(Seconds));
            }
        }
    }
}