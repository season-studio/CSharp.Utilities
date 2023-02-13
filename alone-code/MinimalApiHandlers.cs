using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Localization;
using SeasonStudio.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace SeasonStudio.Web.Common
{
    /// <summary>
    /// 最小API服务载体的基础实现类
    /// </summary>
    public abstract class MinimalApiHandlers
    {
        /// <summary>
        /// 默认的日志输出类，用于无法通过WebApplication获取到日志接口时使用
        /// </summary>
        private class DefaultLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                return null!;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                switch (logLevel)
                {
                    case LogLevel.Error:
                        if (Environment.UserInteractive && (null != Console.Error))
                        {
                            Console.Error.WriteLine(formatter(state, exception));
                        }
                        else
                        {
                            Trace.TraceError(formatter(state, exception));
                        }
                        break;

                    case LogLevel.Warning:
                        if (Environment.UserInteractive && (null != Console.Out))
                        {
                            Console.Out.WriteLine(formatter(state, exception));
                        }
                        else
                        {
                            Trace.TraceWarning(formatter(state, exception));
                        }
                        break;

                    default:
                        if (Environment.UserInteractive && (null != Console.Out))
                        {
                            Console.Out.WriteLine(formatter(state, exception));
                        }
                        else
                        {
                            Trace.TraceInformation(formatter(state, exception));
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 生成默认日志实例
        /// </summary>
        private readonly Lazy<ILogger> defaultLogger = new Lazy<ILogger>(() => new DefaultLogger());

        /// <summary>
        /// 登记关联的应用实例
        /// </summary>
        public readonly WebApplication? Application;

        /// <summary>
        /// 生成关联的日志类实例
        /// </summary>
        private readonly Lazy<ILogger> logger;

        /// <summary>
        /// 关联的日志类
        /// </summary>
        public ILogger Logger => logger.Value;

        /// <summary>
        /// 关联的日志类的生成器
        /// </summary>
        /// <returns></returns>
        private ILogger LoggerGenerator()
        {
            if (null == Application)
            {
                return defaultLogger.Value;
            }

            return (Application.Services.GetService(typeof(ILogger<>).MakeGenericType(this.GetType())) as ILogger) ?? Application.Logger;
        }

        /// <summary>
        /// 全局本地化类
        /// </summary>
        private static IStringLocalizer? globalLocalizer = null;

        /// <summary>
        /// 生成本地化
        /// </summary>
        private readonly Lazy<IStringLocalizer?> localizer;

        /// <summary>
        /// 本地化生成器
        /// </summary>
        /// <returns></returns>
        private IStringLocalizer? LocalizerGenerator()
        {
            if (null == Application)
            {
                return globalLocalizer;
            }

            return Application.Services.GetService<IStringLocalizerFactory>()?.Create(GetType());
        }

        /// <summary>
        /// 获取本地化内容
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_fallback"></param>
        /// <returns></returns>
        public string LocalString(string _name, string? _fallback = null)
        {
            var ret = localizer.Value?.GetString(_name);
            if ((null == ret) || (ret.ResourceNotFound))
            {
                if (null == globalLocalizer)
                {
                    var asm = Assembly.GetEntryAssembly();
                    var asmName = asm?.GetName();
                    if (!string.IsNullOrWhiteSpace(asm?.FullName) && !string.IsNullOrWhiteSpace(asmName?.Name))
                    {
                        var newLocalizer = Application?.Services.GetService<IStringLocalizerFactory>()?.Create(asmName.Name, asm.FullName);
                        Interlocked.Exchange(ref globalLocalizer, newLocalizer);
                    }
                }
                ret = globalLocalizer?.GetString(_name);
            }
            return ((ret?.ResourceNotFound ?? true) ? null : ret.Value) ?? (_fallback ?? _name);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MinimalApiHandlers()
        {
            Application = WorkApplication;
            logger = new Lazy<ILogger>(LoggerGenerator, LazyThreadSafetyMode.PublicationOnly);
            localizer = new Lazy<IStringLocalizer?>(LocalizerGenerator, LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// TLS参数，用于在自动映射最小API实例时，传递关联的Web应用实例
        /// </summary>
        [ThreadStatic]
        private static WebApplication? WorkApplication = null;

        /// <summary>
        /// 映射最小API实例
        /// </summary>
        /// <param name="_app">关联的Web应用实例</param>
        /// <param name="_assembly">被映射的最小API实例类所在的程序集，传入空值表示在当前程序集中检索最小API实例类。默认为空值。</param>
        public static void MapApiHandlers(WebApplication _app, Assembly? _assembly = null)
        {
            if (null == _assembly)
            {
                _assembly = typeof(MinimalApiHandlers).Assembly;
            }

            try
            {
                WorkApplication = _app;
                foreach (var type in _assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(MinimalApiHandlers)))
                    {
                        var obj = Activator.CreateInstance(type);
                        if (null != obj)
                        {
                            foreach (var methodInfo in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                            {
                                var routeAttr = methodInfo.GetCustomAttribute<RouteAttribute>();
                                if (null != routeAttr)
                                {
                                    var methodAttr = methodInfo.GetCustomAttribute<HttpMethodAttribute>();
                                    Type fnType = (methodInfo.ReturnType == typeof(void)) ? Expression.GetActionType(methodInfo.GetParameters().Select(e => e.ParameterType).ToArray()) : Expression.GetFuncType(methodInfo.GetParameters().Select(e => e.ParameterType).Append(methodInfo.ReturnType).ToArray());
                                    Delegate fn = methodInfo.CreateDelegate(fnType, obj);
                                    var route = methodAttr switch
                                    {
                                        HttpGetAttribute => _app.MapGet(routeAttr.Template, fn),
                                        HttpPostAttribute => _app.MapPost(routeAttr.Template, fn),
                                        HttpDeleteAttribute => _app.MapDelete(routeAttr.Template, fn),
                                        HttpPutAttribute => _app.MapPut(routeAttr.Template, fn),
                                        _ => _app.MapGet(routeAttr.Template, fn)
                                    };
                                    if ((null != route) && !string.IsNullOrWhiteSpace(routeAttr.Name))
                                    {
                                        route.WithName(routeAttr.Name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Trace.TraceWarning(err.ToString());
            }
            finally
            {
                WorkApplication = null;
            }
        }
    }
}
