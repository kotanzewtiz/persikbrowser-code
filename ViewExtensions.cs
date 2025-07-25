using Android.Views;

namespace SimpleBrowser
{
    public static class ViewExtensions
    {
        public static void SetPaddingDp(this View view, int leftDp, int topDp, int rightDp, int bottomDp)
        {
            float density = view.Resources.DisplayMetrics.Density;
            view.SetPadding(
                (int)(leftDp * density),
                (int)(topDp * density),
                (int)(rightDp * density),
                (int)(bottomDp * density));
        }
    }
}