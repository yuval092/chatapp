using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using ImageViews.Photo;

namespace Signalir_ChatApp
{
    // Fragment to show image in full-screen
    public class FullScreenImageFragment : Fragment
    {
        private readonly string imagePath;

        public FullScreenImageFragment(string imagePath)
        {
            this.imagePath = imagePath;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Create root layout with MATCH_PARENT to fill the screen
            var rootLayout = new FrameLayout(Context)
            {
                LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            };

            rootLayout.SetBackgroundColor(Color.Black); // Black background for full-screen effect

            // Create PhotoView to display the image
            var photoView = new PhotoView(Context)
            {
                LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
                {
                    Gravity = GravityFlags.Center
                }
            };

            // Load image from file path and set it to the PhotoView
            var bitmap = BitmapFactory.DecodeFile(imagePath);
            photoView.SetImageBitmap(bitmap);

            // Add the PhotoView to the root layout
            rootLayout.AddView(photoView);

            return rootLayout;
        }
    }
}
