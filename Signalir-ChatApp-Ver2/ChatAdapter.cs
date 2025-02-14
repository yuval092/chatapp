using Android.App;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImageViews.Photo;
using System;
using FFImageLoading;
using FFImageLoading.Work;
using FFImageLoading.Transformations;
using Android.Media;
using static System.Net.Mime.MediaTypeNames;
using Android.Text;
using Android.Text.Style;

namespace Signalir_ChatApp
{


    public class ChatAdapter : BaseAdapter<ChatMessage>
    {
        private Activity context;
        private List<ChatMessage> messages;

        public ChatAdapter(Activity context, List<ChatMessage> messages)
        {
            this.context = context;
            this.messages = messages;
        }

        public override ChatMessage this[int position] => messages[position];

        public override int Count => messages.Count;

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var message = messages[position];

            // Inflate custom layout based on incoming or outgoing message
            if (convertView == null)
            {
                convertView = context.LayoutInflater.Inflate(Resource.Layout.chat_item, null);
            }

            var outgoingMessageText = convertView.FindViewById<TextView>(Resource.Id.outgoingMessage);
            var incomingMessageText = convertView.FindViewById<TextView>(Resource.Id.incomingMessage);
            var timeTextRight = convertView.FindViewById<TextView>(Resource.Id.chatTimeRight);
            var timeTextLeft = convertView.FindViewById<TextView>(Resource.Id.chatTimeLeft);
            var incomingPhotoMessage = convertView.FindViewById<ImageView>(Resource.Id.incomingPhotoMessage);
            var incomingPhotoTimeRight = convertView.FindViewById<TextView>(Resource.Id.photoTimeRight);

            var outgoingPhotoMessage = convertView.FindViewById<ImageView>(Resource.Id.outgoingPhotoMessage);
            var outgoingPhotoTimeRight = convertView.FindViewById<TextView>(Resource.Id.photoTimeLeft);

            LinearLayout messageLayout = convertView.FindViewById<LinearLayout>(Resource.Id.incomingMessageLayout);

            //  הודעת טקסט

            if (message.MessageTypeOptions == "Message")
            {      //  הודעה נכנסת
                if (message.IsIncoming == false)
                {
                    //  שורה קצרה ללא אנטר
                    if (!message.Text.Contains("\n"))
                    {
                        var spannable = new SpannableStringBuilder();
                        var text1 = new SpannableString(message.Time);
                        text1.SetSpan(new AbsoluteSizeSpan(10, true), 0, text1.Length(), SpanTypes.ExclusiveExclusive);
                        spannable.Append(text1);

                        spannable.Append(" ");

                        var text2 = new SpannableString(message.Text);
                        text2.SetSpan(new AbsoluteSizeSpan(20, true), 0, text2.Length(), SpanTypes.ExclusiveExclusive);
                        spannable.Append(text2);

                        outgoingMessageText.TextFormatted = spannable;
                        timeTextLeft.Visibility = ViewStates.Gone;

                    }
                    else
                    {
                        outgoingMessageText.Text = message.Text;
                        timeTextLeft.Text = message.Time;
                        timeTextLeft.Visibility = ViewStates.Visible;
                    }
                    //  outgoingMessageText.Text = message.Text;
                    outgoingMessageText.Visibility = ViewStates.Visible; // Show outgoing message
                    incomingMessageText.Visibility = ViewStates.Gone; // Hide incoming message
                    messageLayout.Visibility = ViewStates.Gone;

                    //  timeTextLeft.Text = message.Time;
                    //  timeTextLeft.Visibility = ViewStates.Visible;
                    timeTextRight.Visibility = ViewStates.Gone;

                    incomingPhotoMessage.Visibility = ViewStates.Gone;
                    incomingPhotoTimeRight.Visibility = ViewStates.Gone;
                    outgoingPhotoMessage.Visibility = ViewStates.Gone;
                    outgoingPhotoTimeRight.Visibility = ViewStates.Gone;
                }
                else
                {         //    הודעה יוצאת קצרה 
                    if (!message.Text.Contains("\n"))
                    {
                        var spannable = new SpannableStringBuilder();
                        var text1 = new SpannableString(message.Time);
                        text1.SetSpan(new AbsoluteSizeSpan(10, true), 0, text1.Length(), SpanTypes.ExclusiveExclusive);
                        spannable.Append(text1);

                        spannable.Append(" ");

                        var text2 = new SpannableString(message.Text);
                        text2.SetSpan(new AbsoluteSizeSpan(20, true), 0, text2.Length(), SpanTypes.ExclusiveExclusive);
                        spannable.Append(text2);

                        incomingMessageText.TextFormatted = spannable;
                        timeTextRight.Visibility = ViewStates.Gone;

                    }
                    else
                    {
                        incomingMessageText.Text = message.Text;
                        timeTextLeft.Text = message.Time;
                        timeTextRight.Visibility = ViewStates.Visible;
                    }


                    // incomingMessageText.Text = message.Text;
                    incomingMessageText.Visibility = ViewStates.Visible; // Show incoming message
                    outgoingMessageText.Visibility = ViewStates.Gone; // Hide outgoing message
                    messageLayout.Visibility = ViewStates.Visible;

                    timeTextRight.Text = message.Time;
                    //  timeTextRight.Visibility = ViewStates.Visible;
                    timeTextLeft.Visibility = ViewStates.Gone;

                    incomingPhotoMessage.Visibility = ViewStates.Gone;
                    incomingPhotoTimeRight.Visibility = ViewStates.Gone;
                    outgoingPhotoMessage.Visibility = ViewStates.Gone;
                    outgoingPhotoTimeRight.Visibility = ViewStates.Gone;

                }
            }


            if (message.MessageTypeOptions == "Photo")
            {
                if (message.IsIncoming == false)
                {   //   תמונה מתקבלת 
                    var temp = message;
                    incomingMessageText.Visibility = ViewStates.Gone;
                    outgoingMessageText.Visibility = ViewStates.Gone; // Show outgoing message
                    timeTextLeft.Visibility = ViewStates.Gone;
                    timeTextRight.Visibility = ViewStates.Gone;
                    outgoingPhotoMessage.Visibility = ViewStates.Gone;
                    outgoingPhotoTimeRight.Visibility = ViewStates.Gone;
                    messageLayout.Visibility = ViewStates.Gone;

                    incomingPhotoMessage.Visibility = ViewStates.Visible;
                    incomingPhotoTimeRight.Visibility = ViewStates.Visible;

                    if (System.IO.File.Exists(message.FilePath))
                    {

                        //    טוענים את התמונה בטאסק אסינכרוני כדי לא לתקוע את האפלקציה 
                        //  בנוסף טוענים תמונה ברזולוציה נמוכה מהמקורית 



                        if (message.PortraitOrlandscape == "Portrait")
                        {
                            float density = context.Resources.DisplayMetrics.Density;

                            // Set width and height in dp and convert to pixels
                            int widthInPx = (int)(300 * density);
                            int heightInPx = (int)(300 * density);

                            // Apply layout parameters
                            var layoutParams = incomingPhotoMessage.LayoutParameters;
                            layoutParams.Width = widthInPx;
                            layoutParams.Height = heightInPx;
                            incomingPhotoMessage.LayoutParameters = layoutParams;

                            incomingPhotoMessage.LayoutParameters = layoutParams;
                            ImageService.Instance
                             .LoadFile(message.FilePath)
                             .DownSample(width: 300, height: 300)
                             .Transform(new RotateTransformation(90))
                             .Into(incomingPhotoMessage);


                            incomingPhotoTimeRight.Text = message.Time;
                        }
                        else
                        {   // Photo is LandScape 
                            float density = context.Resources.DisplayMetrics.Density;

                            int widthInPx = (int)(300 * density);
                            int heightInPx = (int)(220 * density);

                            var layoutParams = incomingPhotoMessage.LayoutParameters;
                            layoutParams.Width = widthInPx;
                            layoutParams.Height = heightInPx;
                            incomingPhotoMessage.LayoutParameters = layoutParams;

                            incomingPhotoTimeRight.Text = message.Time;
                            ImageService.Instance
                              .LoadFile(message.FilePath)
                              .DownSample(width: 200, height: 200)
                              .Into(incomingPhotoMessage);
                        }

                    }


                }
                else
                {   //    תמונה נשלחת
                    incomingPhotoMessage.Visibility = ViewStates.Gone;
                    incomingPhotoTimeRight.Visibility = ViewStates.Gone;
                    messageLayout.Visibility = ViewStates.Gone;

                    outgoingPhotoMessage.Visibility = ViewStates.Visible;
                    outgoingPhotoTimeRight.Visibility = ViewStates.Visible;
                    if (System.IO.File.Exists(message.FilePath))
                    {
                        //==============
                        if (message.PortraitOrlandscape == "Portrait")
                        {
                            float density = context.Resources.DisplayMetrics.Density;

                            // Set width and height in dp and convert to pixels
                            int widthInPx = (int)(300 * density);
                            int heightInPx = (int)(300 * density);

                            // Apply layout parameters
                            var layoutParams = outgoingPhotoMessage.LayoutParameters;
                            layoutParams.Width = widthInPx;
                            layoutParams.Height = heightInPx;
                            outgoingPhotoMessage.LayoutParameters = layoutParams;

                            outgoingPhotoMessage.LayoutParameters = layoutParams;
                            ImageService.Instance
                             .LoadFile(message.FilePath)
                             .DownSample(width: 300, height: 300)
                             .Transform(new RotateTransformation(90))
                             .Into(outgoingPhotoMessage);


                            outgoingPhotoTimeRight.Text = message.Time;
                        }
                        else
                        {   // Photo is LandScape 
                            float density = context.Resources.DisplayMetrics.Density;

                            int widthInPx = (int)(300 * density);
                            int heightInPx = (int)(220 * density);

                            var layoutParams = outgoingPhotoMessage.LayoutParameters;
                            layoutParams.Width = widthInPx;
                            layoutParams.Height = heightInPx;
                            outgoingPhotoMessage.LayoutParameters = layoutParams;

                            outgoingPhotoTimeRight.Text = message.Time;
                            ImageService.Instance
                              .LoadFile(message.FilePath)
                              .DownSample(width: 200, height: 200)
                              .Into(outgoingPhotoMessage);
                        }


                        //=============
                    }

                }
            }

            return convertView;
        }



        public void AddMessages(List<ChatMessage> newMessages)
        {
            this.messages.AddRange(newMessages);
            NotifyDataSetChanged();
        }

        public void ClearMessages()
        {
            this.messages.Clear();
            NotifyDataSetChanged();
        }

        //   העלה ברזולוציה נמוכה
        private Bitmap LoadAndResizeBitmap(string filePath, int width, int height)
        {
            var options = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeFile(filePath, options);

            int scale = Math.Min(options.OutWidth / width, options.OutHeight / height);

            options.InSampleSize = scale;
            options.InJustDecodeBounds = false;

            return BitmapFactory.DecodeFile(filePath, options);
        }




        public static void ShowFullScalePhoto(Activity activity, string imagePath)
        {
            var fragmentTransaction = activity.FragmentManager.BeginTransaction();
            var fragment = new FullScreenImageFragment(imagePath);
            fragmentTransaction.Replace(Android.Resource.Id.Content, fragment);
            fragmentTransaction.AddToBackStack(null);
            fragmentTransaction.Commit();



        }

        internal static void ShowFullScalePhoto(MainActivity mainActivity, object imagePath)
        {
            throw new NotImplementedException();
        }
    }


}