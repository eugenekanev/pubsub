using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Bson;

namespace Uptick.Platform.PubSub.Sdk.Extenstions.Aggregator
{
    public class Bucket<TM>
    {
        private class BucketItem
        {
            public TaskCompletionSource<bool> CapturedTask { get; set; }
            public TM MapResult{ get; set; }
        }

        private readonly List<BucketItem> _items;
        public Bucket()
        {
            _items = new List<BucketItem>();
        }

        public void Add(TaskCompletionSource<bool> taskCompletionSource, TM mapResult)
        {
            _items.Add(new BucketItem{CapturedTask = taskCompletionSource, MapResult = mapResult });
        }

        public IEnumerable<TM> Values
        {
            get { return _items.Select(bi => bi.MapResult); }
        }

        public void Fail(Exception ex)
        {
           _items.ForEach(bi => bi.CapturedTask.TrySetException(ex));
        }

        public void Succed()
        {
            _items.ForEach(bi => bi.CapturedTask.TrySetResult(true));
        }
    }
}
