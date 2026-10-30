using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Serializable wrapper for <see cref="DateTime"/> designed specifically for Unity's
/// serialization system, which does not support DateTime natively.
///
/// Key features:
/// - Stores the time internally as ISO 8601 string for stable serialization.
/// - Cached <see cref="DateTime"/> instance to avoid repeated parsing.
/// - Zero GC allocations during runtime access.
/// - Supports safe domain reloads via <see cref="ISerializationCallbackReceiver"/>.
/// </summary>
[Serializable]
public struct SerializableDateTime : ISerializationCallbackReceiver
{
    /// <summary>
    /// The serialized ISO 8601 formatted value,
    /// example: "2025-11-26T10:15:30.0000000Z".
    /// </summary>
    [SerializeField]
    private string _serializedValue;

    /// <summary>
    /// Cached parsed DateTime value. Not serialized.
    /// </summary>
    [NonSerialized]
    private DateTime _cachedValue;

    /// <summary>
    /// Indicates whether the cached DateTime is valid.
    /// </summary>
    [NonSerialized]
    private bool _isCacheValid;

    /// <summary>
    /// ISO 8601 round-trip format. Guaranteed to preserve DateTimeKind.
    /// </summary>
    private static readonly string IsoFormat = "o";

    /// <summary>
    /// Gets or sets the DateTime value represented by this instance.
    /// 
    /// Reading:
    ///     - Parses the serialized string ONCE and caches it.
    ///
    /// Writing:
    ///     - Updates cache AND serialized string immediately.
    ///
    /// Zero allocations on subsequent reads.
    /// </summary>
    public DateTime Value
    {
        get
        {
            if (!_isCacheValid)
            {
                if (string.IsNullOrEmpty(_serializedValue))
                {
                    _cachedValue = default;
                }
                else
                {
                    DateTime.TryParseExact(
                        _serializedValue,
                        IsoFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out _cachedValue
                    );
                }

                _isCacheValid = true;
            }

            return _cachedValue;
        }

        set
        {
            _cachedValue = value;
            _serializedValue = value.ToString(IsoFormat, CultureInfo.InvariantCulture);
            _isCacheValid = true;
        }
    }

    /// <summary>
    /// True if no value has been assigned or serialized.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(_serializedValue);

    /// <summary>
    /// Creates a new <see cref="SerializableDateTime"/> with the given DateTime.
    /// </summary>
    public SerializableDateTime(DateTime dateTime)
    {
        _cachedValue = dateTime;
        _serializedValue = dateTime.ToString(IsoFormat, CultureInfo.InvariantCulture);
        _isCacheValid = true;
    }

    /// <summary>
    /// Syncs cached value into serialized string before Unity serializes the object.
    /// </summary>
    public void OnBeforeSerialize()
    {
        if (_isCacheValid)
        {
            _serializedValue = _cachedValue.ToString(IsoFormat, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Marks cache invalid so the next read reparses the serialized string.
    /// </summary>
    public void OnAfterDeserialize()
    {
        _isCacheValid = false;
    }

    /// <summary>
    /// Returns the ISO 8601 string representation of the DateTime.
    /// </summary>
    public override string ToString()
    {
        return _isCacheValid
            ? _cachedValue.ToString(IsoFormat, CultureInfo.InvariantCulture)
            : _serializedValue;
    }

    public bool IsExpired(DateTime compareTo)
    {
        return Value < compareTo;
    }

    public TimeSpan GetTimeSpan(DateTime compareTo)
    {
        return compareTo - Value;
    }
}
