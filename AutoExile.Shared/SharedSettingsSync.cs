using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace AutoExile
{
    /// <summary>
    /// 모든 독립 플러그인(BossMode, Follower, Blight, Simulacrum, Heist, Labyrinth,
    /// WaveFarm, Idle, HideoutFlow, LootPickupTracker)이 공유하는 "공통 설정"을
    /// 디스크의 단일 JSON 파일(Plugins\Compiled\AutoExileSharedSettings.json)에 동기화합니다.
    ///
    /// 어느 한 플러그인의 설정 메뉴에서 빌드/루팅/위협/맵 기믹/실행 공통/창고/맵 롤링/
    /// 지도 장치/파우스투스/알림 설정을 바꾸면, 다른 모든 플러그인이 주기적으로(약 1초 간격)
    /// 같은 파일을 읽어 자동으로 동일한 값을 반영합니다. 모드 전용 섹션(Boss, Follower,
    /// Blight 등 각 플러그인이 SharedSettings에 추가한 하위 섹션)은 동기화 대상이
    /// 아니며, 플러그인마다 독립적으로 유지됩니다.
    ///
    /// 각 플러그인은 자신만의 AutoExile.Shared.dll 사본을 로드하므로(별도 Resources
    /// 복사본), 이 클래스의 정적(static) 캐시 필드는 플러그인별로 완전히 분리되어
    /// 있습니다 — 서로 다른 플러그인끼리 메모리를 공유하지 않고, 오직 이 JSON 파일을
    /// 통해서만 통신합니다.
    /// </summary>
    public static class SharedSettingsSync
    {
        private const string SharedFileName = "AutoExileSharedSettings.json";
        private const int MinCheckIntervalMs = 1000;

        /// <summary>SharedSettings에서 "공통 설정"으로 간주해 동기화할 최상위 섹션 이름들.</summary>
        private static readonly string[] SyncedSections =
        {
            "Build", "Loot", "Threat", "Mechanics", "Run",
            "Stash", "MapRolling", "MapDevice", "Faustus", "Notifications",
        };

        private static DateTime _lastLoadedWriteTimeUtc = DateTime.MinValue;
        private static string _lastSavedHash = "";
        private static int _lastLoadCheckTick = -MinCheckIntervalMs;
        private static int _lastSaveCheckTick = -MinCheckIntervalMs;

        /// <summary>
        /// 각 플러그인이 설치된 폴더 기준으로 공유 설정 파일 경로를 계산합니다.
        /// 모든 AutoExile.* 플러그인은 Plugins\Compiled\&lt;PluginName&gt;\ 아래에서
        /// 실행되므로, 그 상위 폴더(Plugins\Compiled\)에 공통 파일을 둡니다.
        /// </summary>
        public static string ResolveSharedConfigPath(string pluginDirectory)
        {
            var compiledDir = Directory.GetParent(pluginDirectory)?.FullName ?? pluginDirectory;
            return Path.Combine(compiledDir, SharedFileName);
        }

        /// <summary>
        /// 매 틱 호출해도 안전합니다 — 내부적으로 약 1초 간격으로만 실제 파일을
        /// 확인합니다. 파일이 마지막으로 읽은 시점 이후 갱신되었으면 공통 섹션 값을
        /// 다시 읽어 settings에 적용합니다.
        /// </summary>
        public static void LoadIfChanged(SharedSettings settings, string pluginDirectory)
        {
            if (Environment.TickCount - _lastLoadCheckTick < MinCheckIntervalMs) return;
            _lastLoadCheckTick = Environment.TickCount;

            try
            {
                var path = ResolveSharedConfigPath(pluginDirectory);
                if (!File.Exists(path)) return;

                var writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime <= _lastLoadedWriteTimeUtc) return;

                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);

                foreach (var sectionName in SyncedSections)
                {
                    if (!doc.RootElement.TryGetProperty(sectionName, out var sectionJson)) continue;
                    var prop = typeof(SharedSettings).GetProperty(sectionName, BindingFlags.Public | BindingFlags.Instance);
                    var target = prop?.GetValue(settings);
                    if (target == null) continue;
                    ApplyJsonToObject(sectionJson, target);
                }

                _lastLoadedWriteTimeUtc = writeTime;
                // 방금 읽은 내용을 "이미 저장된 상태"로 기록해 불필요한 즉시 재저장을 방지합니다.
                _lastSavedHash = ComputeHash(settings);
            }
            catch
            {
                // 다른 플러그인이 동시에 쓰는 중이거나 파일이 아직 없는 경우 — 조용히 무시, 다음 주기에 재시도.
            }
        }

        /// <summary>
        /// 매 틱 호출해도 안전합니다 — 내부적으로 약 1초 간격으로만 공통 섹션의 현재
        /// 값을 마지막 저장본과 비교하고, 다르면 공유 파일에 기록합니다.
        /// </summary>
        public static void SaveIfChanged(SharedSettings settings, string pluginDirectory)
        {
            if (Environment.TickCount - _lastSaveCheckTick < MinCheckIntervalMs) return;
            _lastSaveCheckTick = Environment.TickCount;

            try
            {
                var hash = ComputeHash(settings);
                if (hash == _lastSavedHash) return;

                var path = ResolveSharedConfigPath(pluginDirectory);
                var tempPath = path + ".tmp_" + Environment.ProcessId;

                var root = new Dictionary<string, object?>();
                foreach (var sectionName in SyncedSections)
                {
                    var prop = typeof(SharedSettings).GetProperty(sectionName, BindingFlags.Public | BindingFlags.Instance);
                    var target = prop?.GetValue(settings);
                    if (target == null) continue;
                    root[sectionName] = SerializeObject(target);
                }

                var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tempPath, json);
                File.Copy(tempPath, path, overwrite: true);
                File.Delete(tempPath);

                _lastSavedHash = hash;
                _lastLoadedWriteTimeUtc = File.GetLastWriteTimeUtc(path);
            }
            catch
            {
                // 다른 플러그인이 동시에 같은 파일을 쓰고 있을 수 있음 — 다음 주기에 재시도.
            }
        }

        // ================================================================
        // 리플렉션 기반 직렬화/역직렬화 (ExileCore 노드 타입 전용)
        // ================================================================

        private static object? SerializeObject(object obj)
        {
            if (TryGetNodeValue(obj, out var nodeValue)) return nodeValue;

            var dict = new Dictionary<string, object?>();
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
                object? value;
                try { value = prop.GetValue(obj); } catch { continue; }
                if (value == null) continue;
                dict[prop.Name] = SerializeObject(value);
            }
            return dict;
        }

        private static void ApplyJsonToObject(JsonElement json, object target)
        {
            if (TrySetNodeValue(target, json)) return;
            if (json.ValueKind != JsonValueKind.Object) return;

            foreach (var prop in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!json.TryGetProperty(prop.Name, out var childJson)) continue;
                object? childTarget;
                try { childTarget = prop.GetValue(target); } catch { continue; }
                if (childTarget == null) continue;
                ApplyJsonToObject(childJson, childTarget);
            }
        }

        private static bool TryGetNodeValue(object node, out object? value)
        {
            value = null;
            if (node is ExileCore.Shared.Nodes.ToggleNode toggle) { value = toggle.Value; return true; }
            if (node is ExileCore.Shared.Nodes.TextNode text) { value = text.Value ?? ""; return true; }
            if (node is ExileCore.Shared.Nodes.ListNode list) { value = list.Value ?? ""; return true; }

            var type = node.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("RangeNode"))
            {
                var valueProp = type.GetProperty("Value");
                if (valueProp != null) { value = valueProp.GetValue(node); return true; }
            }
            if (type.Name.Contains("HotkeyNode"))
            {
                var valueProp = type.GetProperty("Value");
                value = valueProp?.GetValue(node)?.ToString() ?? "";
                return true;
            }
            return false;
        }

        private static bool TrySetNodeValue(object node, JsonElement json)
        {
            try
            {
                if (node is ExileCore.Shared.Nodes.ToggleNode toggle)
                {
                    if (json.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        toggle.Value = json.GetBoolean();
                    return true;
                }
                if (node is ExileCore.Shared.Nodes.TextNode text)
                {
                    if (json.ValueKind == JsonValueKind.String) text.Value = json.GetString() ?? "";
                    return true;
                }
                if (node is ExileCore.Shared.Nodes.ListNode list)
                {
                    if (json.ValueKind == JsonValueKind.String) list.Value = json.GetString() ?? "";
                    return true;
                }

                var type = node.GetType();
                if (type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("RangeNode"))
                {
                    var genArg = type.GetGenericArguments()[0];
                    var valueProp = type.GetProperty("Value");
                    if (valueProp != null && json.ValueKind == JsonValueKind.Number)
                    {
                        if (genArg == typeof(int)) valueProp.SetValue(node, json.GetInt32());
                        else if (genArg == typeof(float)) valueProp.SetValue(node, json.GetSingle());
                        else if (genArg == typeof(double)) valueProp.SetValue(node, json.GetDouble());
                    }
                    return true;
                }
                if (type.Name.Contains("HotkeyNode"))
                {
                    if (json.ValueKind == JsonValueKind.String &&
                        Enum.TryParse<System.Windows.Forms.Keys>(json.GetString(), true, out var key))
                    {
                        var valueProp = type.GetProperty("Value");
                        valueProp?.SetValue(node, key);
                    }
                    return true;
                }
            }
            catch
            {
                // 타입 불일치 등은 조용히 무시 — 해당 항목만 건너뜁니다.
            }
            return false;
        }

        private static string ComputeHash(SharedSettings settings)
        {
            var root = new Dictionary<string, object?>();
            foreach (var sectionName in SyncedSections)
            {
                var prop = typeof(SharedSettings).GetProperty(sectionName, BindingFlags.Public | BindingFlags.Instance);
                var target = prop?.GetValue(settings);
                if (target == null) continue;
                root[sectionName] = SerializeObject(target);
            }
            return JsonSerializer.Serialize(root);
        }
    }
}
