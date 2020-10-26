using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenRiaServices.Hosting;
using OpenRiaServices.Server;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

namespace TestDomainServices
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/ServerSideAsyncDomainService")]
    public class RangeItem
    {
        [Key]
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Text { get; set; }

        [DataMember]
        public bool ThrowDomainException { get; set; }

        [DataMember]
        public bool ThrowValidationException { get; set; }
    }

    [EnableClientAccess]
    public class ServerSideAsyncDomainService : DomainService
    {
        static readonly List<RangeItem> _items = new List<RangeItem>
            {
                new RangeItem() {Id = 1, Text =  "nr 1"},
                new RangeItem() {Id = 2, Text =  "nr 2"},
                new RangeItem() {Id = 3, Text =  "nr 3"},
                new RangeItem() {Id = 4, Text =  "nr 4"},
                new RangeItem() {Id = 5, Text =  "nr 5"},
            };

        private static TimeSpan _lastDelay;

        private Task Delay(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }

        private Task Delay(TimeSpan delay)
        {
            return Task.Delay(delay);
        }

        public IQueryable<RangeItem> GetRange()
        {
            return _items.AsQueryable();
        }

        /// <summary>
        /// Query returing a queryable range in a task
        /// </summary>
        /// <returns></returns>
        public Task<IQueryable<RangeItem>> GetQueryableRangeAsync()
        {
            return Delay(1)
               .ContinueWith(_ =>
                   _items.AsQueryable()
               );
        }

        /// <summary>
        /// Single item Query throwing exception directly
        /// </summary>
        /// <returns></returns>
        public Task<IQueryable<RangeItem>> GetQueryableRangeWithExceptionFirst()
        {
            throw new DomainException("GetQueryableRangeWithExceptionFirst", 23);
        }

        /// <summary>
        /// Single item Query throwing exception in task
        /// </summary>
        /// <returns></returns>
        public Task<IQueryable<RangeItem>> GetQueryableRangeWithExceptionTask()
        {
            return Delay(2)
                .ContinueWith<IQueryable<RangeItem>>(_ =>
                {
                    throw new DomainException("GetQueryableRangeWithExceptionTask", 24);
                });
        }

        /// <summary>
        /// Query returing a single item in a task
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Task<RangeItem> GetRangeByIdAsync(int id)
        {
            return Delay(1)
                .ContinueWith(_ =>
                    _items.FirstOrDefault(a => a.Id == id)
                );
        }

        /// <summary>
        /// Insert operation
        /// </summary>
        /// <param name="rangeItem">Item to insert</param>
        /// <returns></returns>
        [Insert]
        public async Task InsertRangeAsync(RangeItem rangeItem)
        {
            await Delay(5);
            ThrowExceptionIfSelected(rangeItem, 25);
            rangeItem.Id = 42;
        }

        /// <summary>
        /// Update operation
        /// </summary>
        /// <param name="rangeItem">Item to update</param>
        /// <returns></returns>
        [Update]
        public async Task UpdateRangeAsync(RangeItem rangeItem)
        {
            await Delay(5);
            rangeItem.Text = "updated";
            ThrowExceptionIfSelected(rangeItem, 26);
        }

        /// <summary>
        /// Custom Async update operation that performs a short delay.
        /// </summary>
        /// <param name="rangeItem"></param>
        /// <returns></returns>
        [EntityAction]
        public async Task CustomUpdateRangeAsync(RangeItem rangeItem)
        {
            await Delay(5);
            rangeItem.Text = "custom updated";
            ThrowExceptionIfSelected(rangeItem, 28);
        }

        /// <summary>
        /// Delete operation
        /// </summary>
        /// <param name="rangeItem">Item to delete</param>
        /// <returns></returns>
        [Delete]
        public async Task DeleteRangeAsync(RangeItem rangeItem)
        {
            await Delay(5);
            rangeItem.Text = "deleted";
            ThrowExceptionIfSelected(rangeItem, 27);
        }

        /// <summary>
        /// Single item Query throwing exception directly
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Task<RangeItem> GetRangeByIdWithExceptionFirst(int id)
        {
            throw new DomainException("GetRangeByIdWithExceptionFirst", 23);
        }

        /// <summary>
        /// Single item Query throwing exception in task
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public Task<RangeItem> GetRangeByIdWithExceptionTask(int id)
        {
            return Delay(1)
                .ContinueWith<RangeItem>(_ =>
                {
                    throw new DomainException("GetRangeByIdWithExceptionTask", 24);
                });
        }

        /// <summary>
        /// Sleep the specified delay and then set lastdelay
        /// </summary>
        /// <remarks>
        /// Tests invoke returning simple (void) Task 
        /// </remarks>
        /// <param name="delay">The delay.</param>
        /// <returns></returns>
        [Invoke(HasSideEffects = true)]
        public Task SleepAndSetLastDelay(TimeSpan delay)
        {
            return Delay(delay)
               .ContinueWith(_ =>
               {
                   _lastDelay = delay;
               });
        }

        /// <summary>
        /// Get delay set by SleepAndSetLastDelay
        /// </summary>
        /// <returns>delay set by SleepAndSetLastDelay</returns>
        public TimeSpan GetLastDelay()
        {
            return _lastDelay;
        }

        /// <summary>
        /// Adds one to the number sent by client.
        /// </summary>
        /// <remarks>
        /// Tests invoke returning Task{reference type}
        /// </remarks>
        public Task<string> GreetAsync(string client)
        {
            return Delay(1)
                .ContinueWith(t => string.Format("Hello {0}", client));
        }

        /// <summary>
        /// Adds one to the number sent by client.
        /// </summary>
        /// <remarks>
        /// Tests invoke returning Task{value type}
        /// </remarks>
        public Task<int> AddOneAsync(int number)
        {
            return Delay(1)
                .ContinueWith(t => number + 1);
        }

        /// <summary>
        /// Adds one to the number sent by client.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns></returns>
        /// <remarks>
        /// Tests invoke returning Task{Nullable{T}}
        /// </remarks>
        public Task<int?> AddNullableOneAsync(int? number)
        {
            return Delay(1)
                .ContinueWith(t => number + 1);
        }

        /// <summary>
        /// Invoke throwing exception directly
        /// </summary>
        /// <returns></returns>
        public Task InvokeWithExceptionFirst()
        {
            throw new DomainException("InvokeWithExceptionFirst", 23);
        }

        /// <summary>
        /// Invoke throwing exception directly
        /// </summary>
        /// <returns></returns>
        public Task InvokeWithExceptionTask(int delay)
        {
            return Delay(delay)
                .ContinueWith(_ =>
                {
                    throw new DomainException("InvokeWithExceptionTask", 24);
                });
        }

        private static void ThrowExceptionIfSelected(RangeItem rangeItem, int errorCode, [CallerMemberName] string methodName = null)
        {
            if (rangeItem.ThrowDomainException)
                throw new DomainException(methodName, errorCode);
            else if (rangeItem.ThrowValidationException)
                throw new ValidationException(methodName);
        }
    }
}
