using System.Text.Json;
namespace SaucyMVC.Services;
public static class SessionJson { public static void SetObject<T>(this ISession s,string key,T value)=>s.SetString(key,JsonSerializer.Serialize(value)); public static T? GetObject<T>(this ISession s,string key){var v=s.GetString(key);return v==null?default:JsonSerializer.Deserialize<T>(v);} }
