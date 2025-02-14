using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signalir_ChatApp
{
    public class ConectionAdapter : BaseAdapter<ConnectedUsersList>

    {

        private readonly List<ConnectedUsersList> conectedusers;
        private readonly Activity context;

        public ConectionAdapter(Activity context, List<ConnectedUsersList> conectedusers)
        {
            this.context = context;
            this.conectedusers = conectedusers;
        }

        public override ConnectedUsersList this[int position] => conectedusers[position];

        public override int Count => conectedusers.Count;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            
            var view = convertView ?? context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem1, parent, false);
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text =
                $"{conectedusers[position].Name}";



            return view;
        }

    }
}