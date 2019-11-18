using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using StackExchange.Redis;

namespace SparqDigital.TypTip.Web.Infrastructure.ExtensionMethods
{
     public static class RedisExtensions
     {
          public static IObservable<RedisValue> WhenMessageReceived(this ISubscriber subscriber, RedisChannel channel)
          {
               return Observable.Create<RedisValue>(async (obs, ct) =>
               {
                    await subscriber.SubscribeAsync(channel, (_, message) =>
                    {
                         obs.OnNext(message);
                    }).ConfigureAwait(false);

                    return Disposable.Create(() => subscriber.Unsubscribe(channel));
               });
          }
     }
}