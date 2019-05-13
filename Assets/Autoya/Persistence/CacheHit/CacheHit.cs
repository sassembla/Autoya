using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AutoyaFramework.Persistence.Files;
using UnityEngine.Networking;

namespace AutoyaFramework.Persistence.HashHit
{
    public class HashHit
    {
        private readonly FilePersistence filePersist;
        private Dictionary<string, Dictionary<Char, int>> onMemoryHeadCountCache = new Dictionary<string, Dictionary<Char, int>>();
        private Dictionary<string, Func<string, string>> hmacFuncCache = new Dictionary<string, Func<string, string>>();

        public HashHit(FilePersistence fp)
        {
            this.filePersist = fp;
        }

        private void GenerateCacheIfNeed(string domain)
        {
            if (!hmacFuncCache.ContainsKey(domain))
            {
                var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(domain));
                Func<string, string> onHash = (string input) =>
                {
                    var rawBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
                    return Convert.ToBase64String(rawBytes).Replace("/", "_").Replace("+", "_").Replace("=", "_");
                };
                hmacFuncCache[domain] = onHash;
            }

            if (!onMemoryHeadCountCache.ContainsKey(domain))
            {
                onMemoryHeadCountCache[domain] = new Dictionary<Char, int>();
            }

            if (onMemoryHeadCountCache[domain].Count == 0)
            {
                // オンメモリキャッシュが空なので、ファイルがあればそこから生成する。
                var folderPaths = filePersist.DirectoryNamesInDomain(domain);
                foreach (var folderPath in folderPaths)
                {
                    var initial = folderPath[folderPath.Length - 1];
                    var fileCount = filePersist.FileNamesInDomain(folderPath).Length;
                    onMemoryHeadCountCache[domain][initial] = fileCount;
                }
                return;
            }
        }

        public IEnumerator CacheHashes(
            string domain,
            string[] items,
            Action onSucceeded,
            Action<int, string> onFailed
        )
        {
            var domainPath = Path.Combine(filePersist.basePath, domain);

            if (!filePersist.IsDirectoryExists(domainPath))
            {
                filePersist.CreateDirectory(domainPath);

                // 新規作成なのでオンメモリキャッシュも空
                onMemoryHeadCountCache = new Dictionary<string, Dictionary<Char, int>>();
                onMemoryHeadCountCache[domain] = new Dictionary<Char, int>();
            }

            // フォルダがもうあるので、オンメモリキャッシュが作れる
            GenerateCacheIfNeed(domain);

            // キャッシュがない = ドメイン内のフォルダがない場合、キャッシュが空なので全て生成する。
            if (onMemoryHeadCountCache[domain].Count == 0)
            {
                // これがもっと綺麗に回せればそれでいい気がする。事前にキャッシュがあれば変えられるはず。
                var newHeadCountDict = new Dictionary<Char, List<string>>();
                var distincted = items.Distinct();
                foreach (var item in distincted)
                {
                    // アイテムの長さが0ならキャッシュしない
                    if (item.Length == 0)
                    {
                        continue;
                    }

                    if (!newHeadCountDict.ContainsKey(item[0]))
                    {
                        newHeadCountDict[item[0]] = new List<string>();
                    }
                    newHeadCountDict[item[0]].Add(item);
                }

                // 初回なので全て生成する
                foreach (var key in newHeadCountDict.Keys)
                {
                    // オンメモリキャッシュの更新
                    if (!onMemoryHeadCountCache[domain].ContainsKey(key))
                    {
                        onMemoryHeadCountCache[domain][key] = 0;
                    }

                    var charFolderPath = Path.Combine(domainPath, key.ToString());
                    filePersist.CreateDirectory(charFolderPath);

                    var itemNames = newHeadCountDict[key];
                    foreach (var item in itemNames)
                    {
                        var result = hmacFuncCache[domain](item);
                        filePersist.Update(charFolderPath, result, string.Empty);
                        onMemoryHeadCountCache[domain][key]++;
                    }

                    // おんなじアルファベット始まりの人がそんなに偏らないでしょという油断
                    yield return null;
                }

                onSucceeded();
                yield break;
            }

            // 最低一つはイニシャルフォルダが存在する。その情報はキャッシュに乗っかっている。

            // イニシャルごとに並べる
            var currentHeadCountDict = new Dictionary<Char, HashSet<string>>();
            {
                // この部分を、並び替えだけで軽量化できないかな〜とは思う。毎回全部これ作るの、一瞬とはいえしんどいはず。
                var cmp = StringComparer.OrdinalIgnoreCase;
                Array.Sort(items, cmp);

                foreach (var item in items)
                {
                    // アイテムの長さが0ならキャッシュしない
                    if (item.Length == 0)
                    {
                        continue;
                    }
                    var initial = item[0];
                    if (!currentHeadCountDict.ContainsKey(initial))
                    {
                        currentHeadCountDict[initial] = new HashSet<string>();
                    }
                    currentHeadCountDict[initial].Add(item);
                }
            }

            // イニシャル数自体の比較
            if (currentHeadCountDict.Keys.Count != onMemoryHeadCountCache[domain].Keys.Count)
            {
                // 差がある
                if (currentHeadCountDict.Keys.Count < onMemoryHeadCountCache[domain].Keys.Count)
                {
                    // 消去されたものがある
                    var removedInitial = onMemoryHeadCountCache[domain].Keys.Except(currentHeadCountDict.Keys).FirstOrDefault();

                    // オンメモリキャッシュへからのエントリ削除
                    onMemoryHeadCountCache[domain].Remove(removedInitial);

                    // フォルダー削除
                    var charFolderPath = Path.Combine(domainPath, removedInitial.ToString());
                    filePersist.DeleteDirectory(charFolderPath);
                }
                if (currentHeadCountDict.Keys.Count > onMemoryHeadCountCache[domain].Keys.Count)
                {
                    // 追加されたものがある
                    var addedInitial = currentHeadCountDict.Keys.Except(onMemoryHeadCountCache[domain].Keys).FirstOrDefault();

                    // オンメモリキャッシュへのエントリ追加
                    onMemoryHeadCountCache[domain][addedInitial] = 0;

                    // ファイル生成
                    var charFolderPath = Path.Combine(domainPath, addedInitial.ToString());
                    filePersist.CreateDirectory(charFolderPath);

                    foreach (var item in currentHeadCountDict[addedInitial])
                    {
                        var result = hmacFuncCache[domain](item);
                        filePersist.Update(charFolderPath, result, string.Empty);

                        // オンメモリキャッシュの更新
                        onMemoryHeadCountCache[domain][addedInitial]++;
                    }
                }
                onSucceeded();
                yield break;
            }

            // イニシャル単位での比較
            foreach (var key in currentHeadCountDict.Keys)
            {
                var arrivedLength = currentHeadCountDict[key].Count;

                if (!onMemoryHeadCountCache[domain].ContainsKey(key))
                {
                    // 新しいinitialを持つ集合を発見した。
                    var newItems = currentHeadCountDict[key];
                    var charFolderPath = Path.Combine(domainPath, key.ToString());
                    filePersist.CreateDirectory(charFolderPath);

                    var itemNames = currentHeadCountDict[key];
                    foreach (var item in itemNames)
                    {
                        var result = hmacFuncCache[domain](item);
                        filePersist.Update(charFolderPath, result, string.Empty);
                    }

                    // キャッシュの更新
                    onMemoryHeadCountCache[domain][key] = arrivedLength;

                    // 生成が完了したのでこのブロックを抜ける
                    break;
                }

                // onMemoryHeadCountCache[domain][key]は存在していて、

                // 既知のinitialへのアクセス
                // Debug.Log("k:" + key + " オンメモリにはある、なので変化が発生している");

                // initial内の件数が変わっていない = 変化がない。
                if (arrivedLength == onMemoryHeadCountCache[domain][key])
                {
                    // Debug.Log("key:" + key + " arrivedLength:" + arrivedLength + " onMemoryHeadCountCache[domain][key]:" + onMemoryHeadCountCache[domain][key]);
                    continue;
                }

                // 件数が増えているか減っているか。

                // 減っている -> キャッシュ対象が減った。
                if (arrivedLength < onMemoryHeadCountCache[domain][key])
                {
                    var domainCharPath = Path.Combine(domain, key.ToString());
                    var cachedItems = filePersist.FileNamesInDomain(domainCharPath).Select(p => Path.GetFileName(p)).ToArray();
                    var currentItems = currentHeadCountDict[key].Select(i => hmacFuncCache[domain](i)).ToArray();

                    // 差分ファイル名 = item名を出す
                    var deletedItemName = cachedItems.Except(currentItems).First();

                    // ファイルを削除する
                    filePersist.Delete(domainCharPath, deletedItemName);
                    onMemoryHeadCountCache[domain][key]--;
                    break;
                }

                // 増えている -> キャッシュ対象が増えた。
                if (arrivedLength > onMemoryHeadCountCache[domain][key])
                {
                    var fileNames = currentHeadCountDict[key];

                    var domainCharPath = Path.Combine(domain, key.ToString());
                    var cachedItems = filePersist.FileNamesInDomain(domainCharPath).Select(p => Path.GetFileName(p)).ToArray();
                    var currentItems = currentHeadCountDict[key].Select(i => hmacFuncCache[domain](i)).ToArray();

                    // 差分ファイル名 = item名を出す
                    var newItemName = currentItems.Except(cachedItems).First();

                    // ファイルを出力する
                    filePersist.Update(domainCharPath, newItemName, string.Empty);
                    onMemoryHeadCountCache[domain][key]++;
                    break;
                }
            }

            onSucceeded();
            yield break;
        }

        public bool HitHash(
            string domain,
            string item
        )
        {
            // アイテムの長さが0ならキャッシュ外
            if (item.Length == 0)
            {
                return false;
            }

            // キャッシュ作成(必要であれば)
            GenerateCacheIfNeed(domain);

            var firstChar = item[0];

            // オンメモリキャッシュに対して該当のイニシャルフォルダがあるかどうかチェック
            if (onMemoryHeadCountCache[domain].ContainsKey(firstChar))
            {
                var result = hmacFuncCache[domain](item);

                // ドメイン内にイニシャルフォルダ/ファイルが存在するかチェックする。
                var initialPath = Path.Combine(firstChar.ToString(), result);

                // Debug.Log("initialPath:" + initialPath);
                if (filePersist.IsExist(domain, initialPath))
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearOnMemoryHashCache()
        {
            onMemoryHeadCountCache.Clear();
        }

        public int HashCountByDomain(string domain, Char index)
        {
            if (onMemoryHeadCountCache.ContainsKey(domain))
            {
                if (onMemoryHeadCountCache[domain].ContainsKey(index))
                {
                    return onMemoryHeadCountCache[domain][index];
                }
            }
            return 0;
        }
    }
}
