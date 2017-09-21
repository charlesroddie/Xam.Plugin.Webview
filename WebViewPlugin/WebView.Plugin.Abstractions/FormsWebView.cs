﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xamarin.Forms;
using Xam.Plugin.Abstractions.Extensions;
using Xam.Plugin.Abstractions.Enumerations;
using Xam.Plugin.Abstractions.Events.Inbound;
using Xam.Plugin.Abstractions.Events.Outbound;
using Xam.Plugin.Abstractions.DTO;
using WebView.Plugin.Abstractions.Events.Inbound;

namespace Xam.Plugin.Abstractions
{
    public class FormsWebView : View
    {

        public static readonly BindableProperty NavigatingProperty = BindableProperty.Create(nameof(Navigating), typeof(bool), typeof(FormsWebView), false);
        public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(string), typeof(FormsWebView));
        public static readonly BindableProperty ContentTypeProperty = BindableProperty.Create(nameof(ContentType), typeof(WebViewContentType), typeof(FormsWebView), WebViewContentType.Internet);
        //public static readonly BindableProperty LocalRegisteredActionsProperty = BindableProperty.Create(nameof(LocalRegisteredActions), typeof(Dictionary<string, Action<string>>), typeof(FormsWebView), new Dictionary<string, Action<string>>());
        public static readonly BindableProperty BaseUrlProperty = BindableProperty.Create(nameof(BaseUrl), typeof(string), typeof(FormsWebView));
        public static readonly BindableProperty CanGoBackProperty = BindableProperty.Create(nameof(CanGoBack), typeof(bool), typeof(FormsWebView), false);
        public static readonly BindableProperty CanGoForwardProperty = BindableProperty.Create(nameof(CanGoForward), typeof(bool), typeof(FormsWebView), false);
        public static readonly BindableProperty RequestHeadersProperty = BindableProperty.Create(nameof(RequestHeaders), typeof(IDictionary<string, string>), typeof(FormsWebView), new Dictionary<string, string>());

        private static Dictionary<string, Action<string>> GlobalRegisteredActions = new Dictionary<string, Action<string>>();
        private Dictionary<string, Action<string>> _localRegisteredActions = new Dictionary<string, Action<string>>();

        public string BaseUrl
        {
            get { return (string)GetValue(BaseUrlProperty); }
            set { SetValue(BaseUrlProperty, value); }
        }

        public bool Navigating
        {
            get { return (bool) GetValue(NavigatingProperty); }
        }

        public string Source
        {
            get { return (string) GetValue(SourceProperty); }
            set
            {
                if (value == null) return;
                SetValue(SourceProperty, value);
                WebViewControlDelegate.PerformNavigation(this, value, ContentType);
            }
        }

        public bool CanGoBack
        {
            get { return (bool) GetValue(CanGoBackProperty); }
        }

        public bool CanGoForward
        {
            get { return (bool) GetValue(CanGoForwardProperty); }
        }

        public WebViewContentType ContentType
        {
            get { return (WebViewContentType) GetValue(ContentTypeProperty); }
            set { SetValue(ContentTypeProperty, value); }
        }

        public IDictionary<string, string> RequestHeaders
        {
            get { return (IDictionary<string, string>) GetValue(RequestHeadersProperty); }
            set
            {
                if (value != null)
                    SetValue(RequestHeadersProperty, value);
            }
        }

        Dictionary<string, Action<string>> LocalRegisteredActions
        {
            get { return _localRegisteredActions; }
        }

        public delegate NavigationRequestedDelegate WebViewNavigationStartedEventArgs(NavigationRequestedDelegate eventObj);
        public event WebViewNavigationStartedEventArgs OnNavigationStarted;

        public delegate void WebViewNavigationCompletedEventArgs(NavigationCompletedDelegate eventObj);
        public event WebViewNavigationCompletedEventArgs OnNavigationCompleted;

        public delegate void WebViewNavigationErrorEventArgs(NavigationErrorDelegate eventObj);
        public event WebViewNavigationErrorEventArgs OnNavigationError;

        public delegate void JavascriptResponseEventArgs(JavascriptResponseDelegate eventObj);
        public event JavascriptResponseEventArgs OnJavascriptResponse;

        public delegate void ContentLoadedEventArgs(ContentLoadedDelegate eventObj);
        public event ContentLoadedEventArgs OnContentLoaded;

        public FormsWebView()
        {
        }

        public void GoBack()
        {
            if (CanGoBack)
                WebViewControlDelegate.NavigateThroughStack(this, false);
        }

        public void GoForward()
        {
            if (CanGoForward)
                WebViewControlDelegate.NavigateThroughStack(this, true);
        }

        public void InjectJavascript(string js)
        {
            WebViewControlDelegate.InjectJavascript(this, js);
        }

        [Obsolete("This method's name has been updated to better reflect its use case. Please use the static method RegisterGlobalCallback instead.")]
        public void RegisterCallback(string name, Action<string> callback) => RegisterGlobalCallback(name, callback);

        [Obsolete("This method's name has been updated to better reflect its use case. Please use the static method RemoveGlobalCallback instead.")]
        public void RemoveCallback(string name) => RemoveGlobalCallback(name);

        [Obsolete("This method's name has been updated to better reflect its use case. Please use the static method GetGlobalCallbacks instead.")]
        public string[] GetAllCallbacks() => GetGlobalCallbacks();

        [Obsolete("This method's name has been updated to better reflect its use case. Please use the static method RemoveAllGlobalCallbacks instead.")]
        public void RemoveAllCallbacks() => RemoveAllGlobalCallbacks();

        public static void RegisterGlobalCallback(string name, Action<string> callback)
        {
            if (GlobalRegisteredActions.ContainsKey(name)) return;
            GlobalRegisteredActions.Add(name, callback);
            WebViewControlDelegate.NotifyCallbacksChanged(null, name, true);
        }

        public static void RemoveGlobalCallback(string name)
        {
            if (GlobalRegisteredActions.ContainsKey(name))
                GlobalRegisteredActions.Remove(name);
        }

        public static string[] GetGlobalCallbacks()
        {
            return GlobalRegisteredActions.Keys.ToArray();
        }

        public static void RemoveAllGlobalCallbacks()
        {
            GlobalRegisteredActions.Clear();
        }

        public void RegisterLocalCallback(string name, Action<string> callback)
        {
            if (LocalRegisteredActions.ContainsKey(name)) return;
            LocalRegisteredActions.Add(name, callback);
            WebViewControlDelegate.NotifyCallbacksChanged(this, name, false);
        }

        public void RemoveLocalCallback(string name)
        {
            if (LocalRegisteredActions.ContainsKey(name))
                LocalRegisteredActions.Remove(name);
        }

        public string[] GetLocalCallbacks()
        {
            return LocalRegisteredActions.Keys.ToArray();
        }

        public void RemoveAllLocalCallbacks()
        {
            LocalRegisteredActions.Clear();
        }

        /// <summary>
        /// Internal Use Only.
        /// </summary>
        /// <param name="type">The type of event</param>
        /// <param name="eventObject">The WVE object to pass</param>
        /// <returns></returns>
        public object InvokeEvent(WebViewEventType type, WebViewDelegate eventObject)
        {
            switch (type)
            {
                case WebViewEventType.NavigationRequested:
                    SetValue(NavigatingProperty, true);
                    return OnNavigationStarted?.Invoke(eventObject as NavigationRequestedDelegate);

                case WebViewEventType.NavigationComplete:
                    SetValue(NavigatingProperty, false);
                    OnNavigationCompleted?.Invoke(eventObject as NavigationCompletedDelegate);
                    break;

                case WebViewEventType.NavigationError:
                    SetValue(NavigatingProperty, false);
                    OnNavigationError?.Invoke(eventObject as NavigationErrorDelegate);
                    break;

                case WebViewEventType.ContentLoaded:
                    SetValue(NavigatingProperty, false);
                    OnContentLoaded?.Invoke(eventObject as ContentLoadedDelegate);
                    break;

                case WebViewEventType.NavigationStackUpdate:
                    var stackUpdateDelegate = eventObject as NavigationStackUpdateDelegate;
                    var navigationStackUpdateDelegate = eventObject as NavigationStackUpdateDelegate;

                    if (stackUpdateDelegate != null)
                        SetValue(CanGoBackProperty, stackUpdateDelegate.CanGoBack);

                    if (navigationStackUpdateDelegate != null)
                        SetValue(CanGoForwardProperty, navigationStackUpdateDelegate.CanGoForward);
                    break;

                case WebViewEventType.JavascriptCallback:
                    var javascriptResponseDelegate = eventObject as JavascriptResponseDelegate;
                    if (javascriptResponseDelegate != null)
                    {
                        var data = javascriptResponseDelegate.Data;
                        ActionResponse ar;
                        if (data.ValidateJSON() && (ar = data.AttemptParseActionResponse()) != null)
                        {
                            // Attempt Locals
                            if (LocalRegisteredActions.ContainsKey(ar.Action))
                                LocalRegisteredActions[ar.Action]?.Invoke(ar.Data);

                            // Attempt Globals
                            if (GlobalRegisteredActions.ContainsKey(ar.Action))
                                GlobalRegisteredActions[ar.Action]?.Invoke(ar.Data);
                        }
                        else
                        {
                            OnJavascriptResponse?.Invoke((JavascriptResponseDelegate) eventObject);
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            
            return eventObject;
        }
    }
}
