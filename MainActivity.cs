using Android.App;
using Android.OS;
using Android.Webkit;
using Android.Widget;
using Android.Views;
using Android.Graphics;
using Android.Text;
using Android.Content.PM;
using Android.Content;
using Android.Views.InputMethods;
using Android.Runtime;
using Android.Net;
using Android.Util;
using System;
using System.Collections.Generic;

namespace SimpleBrowser
{
    [Activity(Label = "SimpleBrowser", MainLauncher = true,
        Theme = "@android:style/Theme.Material.Light.NoActionBar",
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity
    {
        WebView webView = null!;
        EditText urlEdit = null!;
        ImageButton backButton = null!;
        ImageButton forwardButton = null!;
        ImageButton refreshButton = null!;
        ImageButton menuButton = null!;
        ImageButton clearUrlButton = null!;
        ProgressBar progressBar = null!;

        List<string> historyUrls = new List<string>();
        int historyIndex = -1;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // --- Полноэкранный режим ---
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
                SystemUiFlags.LayoutStable
                | SystemUiFlags.LayoutHideNavigation
                | SystemUiFlags.LayoutFullscreen
                | SystemUiFlags.HideNavigation
                | SystemUiFlags.Fullscreen
                | SystemUiFlags.ImmersiveSticky);

            // --- Основной layout ---
            LinearLayout mainLayout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MatchParent,
                    LinearLayout.LayoutParams.MatchParent)
            };
            mainLayout.SetPadding(DpToPx(0), DpToPx(0), DpToPx(0), DpToPx(0));

            // --- Верхняя панель ---
            LinearLayout topPanel = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MatchParent,
                    LinearLayout.LayoutParams.WrapContent)
            };
            topPanel.SetBackgroundColor(Color.ParseColor("#FAFAFA"));
            topPanel.SetPadding(DpToPx(8), DpToPx(8), DpToPx(8), DpToPx(8));
            topPanel.Elevation = 8f;

            urlEdit = new EditText(this)
            {
                Hint = "Введите адрес или запрос",
                TextSize = 16f,
                InputType = InputTypes.ClassText | InputTypes.TextVariationUri,
                ImeOptions = ImeAction.Go,
                Ellipsize = TextUtils.TruncateAt.End,
                LayoutParameters = new LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WrapContent, 1f)
            };
            urlEdit.SetSingleLine(true);
            urlEdit.SetHorizontallyScrolling(true);
            urlEdit.SetPadding(DpToPx(12), DpToPx(8), DpToPx(12), DpToPx(8));
            urlEdit.EditorAction += (s, e) =>
            {
                if (e.ActionId == ImeAction.Go)
                {
                    LoadUrlFromEdit();
                    e.Handled = true;
                }
            };
            topPanel.AddView(urlEdit);

            clearUrlButton = CreateButton(Android.Resource.Drawable.IcMenuCloseClearCancel);
            clearUrlButton.SetPadding(DpToPx(12), DpToPx(12), DpToPx(12), DpToPx(12));
            clearUrlButton.Click += (s, e) => { urlEdit.Text = ""; };
            topPanel.AddView(clearUrlButton, new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WrapContent, LinearLayout.LayoutParams.WrapContent));

            mainLayout.AddView(topPanel);

            // --- Прогрессбар ---
            progressBar = new ProgressBar(this, null, Android.Resource.Attribute.ProgressBarStyleHorizontal);
            progressBar.LayoutParameters = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent, DpToPx(6));
            progressBar.Max = 100;
            progressBar.Visibility = ViewStates.Gone;
            mainLayout.AddView(progressBar);

            // --- WebView ---
            webView = new WebView(this);
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.DomStorageEnabled = true;
            webView.Settings.LoadWithOverviewMode = true;
            webView.Settings.UseWideViewPort = true;
            webView.Settings.MixedContentMode = MixedContentHandling.CompatibilityMode; // более безопасно
            webView.Settings.CacheMode = CacheModes.NoCache;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBean)
            {
                webView.Settings.AllowFileAccessFromFileURLs = false;
                webView.Settings.AllowUniversalAccessFromFileURLs = false;
            }

            webView.Settings.AllowContentAccess = false;
            webView.Settings.SavePassword = false;
            webView.Settings.SaveFormData = false;

            webView.SetWebChromeClient(new MyWebChromeClient(this));
            webView.SetWebViewClient(new MyWebViewClient(this));

            // Поддержка загрузок файлов через интерфейс IDownloadListener
            webView.SetDownloadListener(new MyDownloadListener(this));

            var webViewParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent,
                0, 1f);
            mainLayout.AddView(webView, webViewParams);

            // --- Нижняя панель ---
            LinearLayout bottomPanel = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MatchParent,
                    LinearLayout.LayoutParams.WrapContent)
            };
            bottomPanel.SetBackgroundColor(Color.ParseColor("#EEEEEE"));
            bottomPanel.SetPadding(DpToPx(4), DpToPx(4), DpToPx(4), DpToPx(4));
            bottomPanel.Elevation = 8f;

            backButton = CreateButton(Android.Resource.Drawable.IcMediaPrevious);
            forwardButton = CreateButton(Android.Resource.Drawable.IcMediaNext);
            refreshButton = CreateButton(Android.Resource.Drawable.IcPopupSync);
            menuButton = CreateButton(Android.Resource.Drawable.IcMenuMore);

            backButton.Click += (s, e) =>
            {
                if (webView.CanGoBack())
                    webView.GoBack();
                else
                    Toast.MakeText(this, "Нет предыдущей страницы", ToastLength.Short).Show();
            };

            forwardButton.Click += (s, e) =>
            {
                if (webView.CanGoForward())
                    webView.GoForward();
                else
                    Toast.MakeText(this, "Нет следующей страницы", ToastLength.Short).Show();
            };

            refreshButton.Click += (s, e) => webView.Reload();

            menuButton.Click += (s, e) => ShowSettingsMenu();

            int btnSize = DpToPx(48);

            bottomPanel.AddView(backButton, new LinearLayout.LayoutParams(btnSize, btnSize));
            bottomPanel.AddView(forwardButton, new LinearLayout.LayoutParams(btnSize, btnSize));
            bottomPanel.AddView(refreshButton, new LinearLayout.LayoutParams(btnSize, btnSize));
            bottomPanel.AddView(menuButton, new LinearLayout.LayoutParams(btnSize, btnSize));

            mainLayout.AddView(bottomPanel);

            SetContentView(mainLayout);

            LoadUrl("https://yandex.ru");
        }

        int DpToPx(int dp) => (int)(dp * Resources.DisplayMetrics.Density);

        void LoadUrlFromEdit()
        {
            string url = urlEdit.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                Toast.MakeText(this, "Введите URL", ToastLength.Short).Show();
                return;
            }

            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                string query = Android.Net.Uri.Encode(url);
                url = $"https://yandex.ru/search/?text={query}";
            }

            LoadUrl(url);
            HideKeyboard();
        }

        void LoadUrl(string url)
        {
            webView.LoadUrl(url);
            UpdateUrl(url);
            SaveToHistory(url);
        }

        void SaveToHistory(string url)
        {
            if (historyIndex == -1 || historyIndex == historyUrls.Count - 1)
            {
                historyUrls.Add(url);
                historyIndex = historyUrls.Count - 1;
            }
            else
            {
                int removeCount = historyUrls.Count - historyIndex - 1;
                if (removeCount > 0)
                    historyUrls.RemoveRange(historyIndex + 1, removeCount);
                historyUrls.Add(url);
                historyIndex = historyUrls.Count - 1;
            }
        }

        void ShowSettingsMenu()
        {
            PopupMenu popup = new PopupMenu(this, menuButton);
            popup.Menu.Add("Настройки");
            popup.Menu.Add("О программе");
            popup.Menu.Add("Очистить историю");
            popup.Menu.Add("Закрыть");

            popup.MenuItemClick += (s, e) =>
            {
                string? title = e.Item?.TitleFormatted?.ToString();
                if (title == null) return;

                switch (title)
                {
                    case "Настройки":
                        ShowSettingsDialog();
                        break;
                    case "О программе":
                        ShowAboutDialog();
                        break;
                    case "Очистить историю":
                        historyUrls.Clear();
                        historyIndex = -1;
                        webView.ClearHistory();
                        Toast.MakeText(this, "История очищена", ToastLength.Short).Show();
                        break;
                    case "Закрыть":
                        Finish();
                        break;
                }
            };
            popup.Show();
        }

        void ShowSettingsDialog()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Настройки");

            bool jsEnabled = webView.Settings.JavaScriptEnabled;

            builder.SetMultiChoiceItems(new string[] { "Включить JavaScript" }, new bool[] { jsEnabled },
                (sender, args) =>
                {
                    webView.Settings.JavaScriptEnabled = args.IsChecked;
                    Toast.MakeText(this, $"JavaScript {(args.IsChecked ? "включен" : "выключен")}", ToastLength.Short).Show();
                });

            builder.SetPositiveButton("ОК", (s, e) => { });
            builder.SetNegativeButton("Отмена", (s, e) => { });
            builder.Show();
        }

        void ShowAboutDialog()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("О программе");
            builder.SetMessage("Простой браузер на Xamarin.Android\nВерсия 1.2\n© 2025\nАвтор: Ваше Имя");
            builder.SetPositiveButton("ОК", (s, e) => { });
            builder.Show();
        }

        public void UpdateUrl(string url)
        {
            urlEdit.Text = url;
        }

        void HideKeyboard()
        {
            InputMethodManager imm = (InputMethodManager)GetSystemService(InputMethodService);
            imm?.HideSoftInputFromWindow(urlEdit.WindowToken, 0);
        }

        private ImageButton CreateButton(int drawableId)
        {
            var btn = new ImageButton(this)
            {
                Background = null
            };
            btn.SetImageResource(drawableId);
            btn.SetScaleType(ImageView.ScaleType.CenterInside);
            btn.SetPadding(DpToPx(8), DpToPx(8), DpToPx(8), DpToPx(8));

            // Ripple effect
            var typedValue = new TypedValue();
            Theme.ResolveAttribute(Android.Resource.Attribute.SelectableItemBackgroundBorderless, typedValue, true);
            btn.SetBackgroundResource(typedValue.ResourceId);
            return btn;
        }

        // --- Класс для обработки загрузок ---
        private class MyDownloadListener : Java.Lang.Object, IDownloadListener
        {
            readonly MainActivity activity;

            public MyDownloadListener(MainActivity activity)
            {
                this.activity = activity;
            }

            public void OnDownloadStart(string url, string userAgent, string contentDisposition, string mimeType, long contentLength)
            {
                try
                {
                    var request = new DownloadManager.Request(Android.Net.Uri.Parse(url));
                    request.SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted);

                    string fileName = URLUtil.GuessFileName(url, contentDisposition, mimeType);
                    request.SetDestinationInExternalPublicDir(Android.OS.Environment.DirectoryDownloads, fileName);

                    var dm = (DownloadManager)activity.GetSystemService(DownloadService);
                    dm.Enqueue(request);

                    Toast.MakeText(activity, $"Скачивание началось: {fileName}", ToastLength.Short).Show();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(activity, $"Ошибка при запуске загрузки: {ex.Message}", ToastLength.Long).Show();
                }
            }
        }

        private class MyWebViewClient : WebViewClient
        {
            readonly MainActivity activity;

            public MyWebViewClient(MainActivity activity)
            {
                this.activity = activity;
            }

            public override void OnPageFinished(WebView? view, string? url)
            {
                base.OnPageFinished(view, url);
                if (!string.IsNullOrEmpty(url))
                {
                    activity.RunOnUiThread(() =>
                    {
                        activity.UpdateUrl(url);
                        activity.progressBar.Progress = 0;
                        activity.progressBar.Visibility = ViewStates.Gone;
                        activity.backButton.Enabled = activity.webView.CanGoBack();
                        activity.forwardButton.Enabled = activity.webView.CanGoForward();
                    });
                }
            }

            public override bool ShouldOverrideUrlLoading(WebView? view, IWebResourceRequest? request)
            {
                if (request != null && request.Url != null)
                {
                    view?.LoadUrl(request.Url.ToString());
                    return true;
                }
                return false;
            }

            public override void OnReceivedError(WebView? view, IWebResourceRequest? request, WebResourceError? error)
            {
                base.OnReceivedError(view, request, error);
                activity.RunOnUiThread(() =>
                {
                    Toast.MakeText(activity, $"Ошибка загрузки: {error?.Description}", ToastLength.Short).Show();
                });
            }
        }

        private class MyWebChromeClient : WebChromeClient
        {
            readonly MainActivity activity;

            public MyWebChromeClient(MainActivity activity)
            {
                this.activity = activity;
            }

            public override void OnProgressChanged(WebView? view, int newProgress)
            {
                base.OnProgressChanged(view, newProgress);
                activity.RunOnUiThread(() =>
                {
                    activity.progressBar.Progress = newProgress;
                    activity.progressBar.Visibility = newProgress < 100 ? ViewStates.Visible : ViewStates.Gone;
                });
            }
        }
    }
}
