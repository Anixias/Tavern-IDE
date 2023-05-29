using System.Collections;

namespace Glint.Collections.Generic;

public sealed class Catalog<TKey, TValue> :
	ICollection<KeyValuePair<TKey, TValue>>,
	IDictionary<TKey, TValue>,
	IEnumerable<KeyValuePair<TKey, TValue>>
		where TKey : notnull
		where TValue : notnull
{
	private readonly Dictionary<TKey, TValue> keyDictionary = new();
	private readonly Dictionary<TValue, TKey> valueDictionary = new();

	public bool IsReadOnly => false;

	public int Count
	{
		get
		{
			lock (syncLock)
			{
				return keyDictionary.Count;
			}
		}
	}
	public ICollection<TKey> Keys
	{
		get
		{
			lock (syncLock)
			{
				return keyDictionary.Keys;
			}
		}
	}
	public ICollection<TValue> Values
	{
		get
		{
			lock (syncLock)
			{
				return keyDictionary.Values;
			}
		}
	}
	
	private readonly object syncLock = new();

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
	{
		
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> pair)
	{
		return Remove(pair.Key);
	}

	public void Add(KeyValuePair<TKey, TValue> pair)
	{
		Add(pair.Key, pair.Value);
	}

	public void Add(TKey key, TValue value)
	{
		if (key is null)
			throw new ArgumentNullException(nameof(key));

		if (value is null)
			throw new ArgumentNullException(nameof(value));
		
		lock (syncLock)
		{
			if (keyDictionary.ContainsKey(key))
			{
				throw new ArgumentException("An element with the same key already exists.", nameof(key));
			}

			if (valueDictionary.ContainsKey(value))
			{
				throw new ArgumentException("An element with the same value already exists.", nameof(value));
			}

			keyDictionary.Add(key, value);
			valueDictionary.Add(value, key);
		}
	}

	public bool ContainsKey(TKey key)
	{
		lock (syncLock)
		{
			return keyDictionary.ContainsKey(key);
		}
	}
	
	public bool ContainsValue(TValue value)
	{
		lock (syncLock)
		{
			return valueDictionary.ContainsKey(value);
		}
	}
	
	public bool Contains(KeyValuePair<TKey, TValue> pair)
	{
		lock (syncLock)
		{
			if (keyDictionary.TryGetValue(pair.Key, out var value))
				return value.Equals(pair.Value);
		}

		return false;
	}

	public bool TryAdd(TKey key, TValue value)
	{
		try
		{
			Add(key, value);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		try
		{
			value = this[key];
			return true;
		}
		catch (Exception)
		{
			value = default!;
			return false;
		}
	}

	public bool TryGetKey(TValue value, out TKey key)
	{
		try
		{
			key = this[value];
			return true;
		}
		catch (Exception)
		{
			key = default!;
			return false;
		}
	}

	public void Clear()
	{
		lock (syncLock)
		{
			keyDictionary.Clear();
			valueDictionary.Clear();
		}
	}

	public bool Remove(TKey key)
	{
		if (!ContainsKey(key))
			return false;
		
		lock (syncLock)
		{
			valueDictionary.Remove(keyDictionary[key]);
			keyDictionary.Remove(key);
		}

		return true;

	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		lock (syncLock)
		{
			return keyDictionary.GetEnumerator();
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		lock (syncLock)
		{
			return keyDictionary.GetEnumerator();
		}
	}

	public override int GetHashCode()
	{
		lock (syncLock)
		{
			var valueHashCode = valueDictionary.GetHashCode();
			return keyDictionary.GetHashCode() | (valueHashCode << 2) | (valueHashCode >> 30);
		}
	}
	
	public TValue GetValue(TKey key)
	{
		lock (syncLock)
		{
			return keyDictionary[key];
		}
	}
	
	public void SetValue(TKey key, TValue value)
	{
		Remove(key);
		Add(key, value);
	}

	public TKey GetKey(TValue keyValue)
	{
		lock (syncLock)
		{
			return valueDictionary[keyValue];
		}
	}

	public void SetKey(TValue keyValue, TKey key)
	{
		Remove(key);
		Add(key, keyValue);
	}

	public TValue this[TKey key]
	{
		get => GetValue(key);
		set => SetValue(key, value);
	}

	public TKey this[TValue keyValue]
	{
		get => GetKey(keyValue);
		set => SetKey(keyValue, value);
	}
}