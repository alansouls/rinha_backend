using System.Collections.Concurrent;
using RinhaBackend.Shared.Dtos.Payments;

namespace RinhaBackend.Shared.Utils;

public class MyMemDb
{
    private readonly ConcurrentDictionary<Guid, PaymentUpdated> _paymentDto = [];
    private readonly SortedList<DateTimeOffset, List<PaymentUpdated>> _paymentDtosByTimeStamp = [];

    public void Insert(PaymentUpdated dto)
    {
        if (!_paymentDto.TryAdd(dto.Id, dto))
        {
            return;
        }

        if (_paymentDtosByTimeStamp.TryGetValue(dto.TimeStamp, out var list))
        {
            list.Add(dto);
        }
        else
        {
            _paymentDtosByTimeStamp[dto.TimeStamp] = [dto];
        }
    }

    public IEnumerable<PaymentUpdated> GetPaymentDtos(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (_paymentDtosByTimeStamp.Count == 0)
        {
            return [];
        }
        
        var startIndex = from.HasValue ? BinarySearchClosestIndex(_paymentDtosByTimeStamp, from.Value) : 0;
        var endIndex = to.HasValue
            ? BinarySearchClosestIndex(_paymentDtosByTimeStamp, to.Value, ClosestSearchMode.Lesser)
            : _paymentDtosByTimeStamp.Count - 1;

        return _paymentDtosByTimeStamp.Values.ToArray()[startIndex..(endIndex + 1)].SelectMany(v => v);
    }

    private enum ClosestSearchMode
    {
        Lesser,
        Greater
    }

    private static int BinarySearchClosestIndex<TKey, TValue>(
        SortedList<TKey, TValue> sortedList,
        TKey targetKey,
        ClosestSearchMode mode = ClosestSearchMode.Greater
    ) where TKey : IComparable<TKey>
    {
        var low = 0;
        var high = sortedList.Count - 1;
        var resultIndex = -1;

        while (low <= high)
        {
            var mid = (low + high) / 2;
            var midKey = sortedList.Keys[mid];
            var comparison = midKey.CompareTo(targetKey);

            switch (comparison)
            {
                case 0:
                    return mid;
                case < 0:
                {
                    if (mode == ClosestSearchMode.Lesser)
                        resultIndex = mid;

                    low = mid + 1;
                    break;
                }
                default:
                {
                    if (mode == ClosestSearchMode.Greater)
                        resultIndex = mid;

                    high = mid - 1;
                    break;
                }
            }
        }

        return resultIndex;
    }
}