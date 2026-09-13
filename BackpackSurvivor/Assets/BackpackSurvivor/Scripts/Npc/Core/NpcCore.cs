using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BS.Quest;

namespace BS.Npc
{
    public enum DialogueSurface { Camp, Pulse, Settlement }
    public enum DialogueIntent { Conversation, Facts, Restricted }

    [Serializable]
    public sealed class NpcFacts
    {
        public string contractId, contractTitle, backpack, run, campaign, stage;
        public int tier;
        public string[] objectives, progress, items;
        public string[] definitionIds, definitions;
        public string[] knownItemNames;
        public string verdict;
        public string Resolve(string kind, int index)
        {
            if (kind == "objective") return objectives != null && index >= 0 && index < objectives.Length ? objectives[index] : null;
            if (kind == "progress") return progress != null && index >= 0 && index < progress.Length ? progress[index] : null;
            if (kind == "definition") return definitions != null && index >= 0 && index < definitions.Length ? definitions[index] : null;
            if (kind == "item") return items != null && index >= 0 && index < items.Length ? items[index] : null;
            if (index != 0) return null;
            switch (kind) { case "backpack": return backpack; case "run": return run; case "contract": return contractTitle; case "campaign": return campaign; case "stage": return stage; default: return null; }
        }
    }

    public static class FactBlockBuilder
    {
        public static NpcFacts Capture(QuestInstance quest, QuestRunSnapshot snapshot, int completedEvents = 0, string stage = null, IEnumerable<ItemRecord> itemDefinitions = null)
        {
            var q = quest?.Copy();
            var clauses = q?.objectives ?? new List<ObjectiveClause>();
            var records = snapshot?.items ?? new List<ItemRecord>();
            var catalog = (itemDefinitions ?? new ItemRecord[0]).ToArray();
            var local = snapshot == null || q == null ? null : QuestEvaluator.Evaluate(q, snapshot);
            var result = new NpcFacts {
                stage = stage,
                contractId = q?.eventId ?? "", tier = q?.tier ?? 0,
                contractTitle = q?.briefingTitle ?? "当前没有进行中的合同",
                objectives = clauses.Select(ObjectiveText.Format).ToArray(),
                progress = clauses.Select((c,i) => "目标" + (i+1) + "：" + (local != null && i < local.Details.Count && local.Details[i].satisfied ? "当前条件满足；完成以胜利结算为准" : "当前条件未满足")).ToArray(),
                items = records.Select(i => i.id + "，等级 " + i.level + "，价值 " + i.scoreValue).ToArray(),
                definitionIds = catalog.Select(i=>i.id).ToArray(),
                definitions = catalog.Select(i=>i.id+"，类别 "+ObjectiveText.Tag(i.tag)+"，品质 "+ObjectiveText.Quality((int)i.rarity)+"，基础价值 "+i.scoreValue).ToArray(),
                knownItemNames = catalog.Select(i=>i.id).Concat(records.Select(i=>i.id)).Concat(clauses.Select(c=>c?.itemId)).Concat(clauses.Where(c=>c?.itemIds!=null).SelectMany(c=>c.itemIds)).Where(s=>!string.IsNullOrWhiteSpace(s)).Distinct().ToArray(),
                backpack = snapshot == null ? "当前背包：空；尚未开始本局。" : "当前背包 " + records.Count + " 件，价值 " + snapshot.backpackValue + "。",
                run = snapshot == null ? "尚未开始本局。" : "已记录存活 " + (int)snapshot.elapsed + " 秒，等级 " + snapshot.level + "，击杀 " + snapshot.kills + "，精英击杀 " + snapshot.eliteKills + "。" + (stage == null ? "" : "当前阶段：" + stage + "。"),
                campaign = "当前合同层级 " + (q?.tier ?? 0) + "，已完成事件 " + completedEvents + "。",
                verdict = local?.Completed == true ? "complete" : snapshot?.outcome == RunOutcome.Died ? "failed" : "partial"
            };
            return result;
        }
        public static string Build(QuestInstance quest, QuestRunSnapshot snapshot)
        {
            var f=Capture(quest,snapshot);
            return "事实区（只读）：" + f.contractTitle + "\n" + string.Join("\n",f.objectives) + "\n" + f.backpack + "\n" + f.run;
        }
    }

    public static class DialogueRouter
    {
        static readonly string[] restricted = { "概率", "掉落率", "必出", "直接给", "给我物品", "跳过", "忽略规则", "忽略之前", "修改存档", "系统提示", "提示词", "token", "api key", "色情", "裸照", "毒品", "赌博", "种族歧视", "暴力血腥" };
        public static bool TryRoute(DialogueSurface surface, out int maxChars)
        { maxChars=surface==DialogueSurface.Pulse?60:600; return Enum.IsDefined(typeof(DialogueSurface),surface); }
        public static bool IsHistoryQuery(string input)
        {
            if(string.IsNullOrWhiteSpace(input)) return false;
            return new[]{"上一趟","上次","经历","战绩","记得","带回","死亡后","胜利后","历史","之前","过去","曾经","哪一局","哪次"}.Any(input.Contains);
        }
        public static DialogueIntent Classify(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length>1000 || input.IndexOfAny(new[]{'<','>','\0'})>=0 || restricted.Any(w=>input.IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0)) return DialogueIntent.Restricted;
            return new[]{"任务","合同","目标","背包","进度","物品","等级","价值","击杀","上一趟","上次","经历","战绩","记得","带回","死亡","胜利","历史","之前","过去","曾经"}.Any(input.Contains) ? DialogueIntent.Facts : DialogueIntent.Conversation;
        }
    }

    public static class NpcResponseValidator
    {
        static readonly Regex references = new Regex(@"\[\[(objective|progress|item|definition|backpack|run|contract|campaign|stage):(\d+)\]\]",RegexOptions.CultureInvariant);
        static readonly string[] forbidden = { "概率", "掉落率", "一定", "保证", "下次", "必出", "已完成合同", "合同已完成", "任务已完成", "未完成任务", "直接给", "跳过任务", "修改存档", "改变掉落", "忽略规则", "系统提示", "语言模型", "DeepSeek", "token", "色情", "毒品", "赌博", "提前撤离", "商店", "多人", "Boss", "局外成长" };
        public static bool IsSafe(string response, int maxChars=600)
        {
            return !string.IsNullOrWhiteSpace(response) && response.Length<=maxChars && response.IndexOfAny(new[]{'<','>','\0','\uFFFD'})<0 && !response.Contains("{{") && !response.Contains("{count}") && !forbidden.Any(w=>response.IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0);
        }
        public static bool TryRenderFreeText(string template, int maxChars, out string rendered)
        {
            rendered=null;
            if (!IsFreeTextSafe(template, maxChars)) return false;
            rendered=template.Trim();
            return true;
        }
        public static bool IsFreeTextSafe(string response, int maxChars=600)
        {
            if (string.IsNullOrWhiteSpace(response) || response.Length>maxChars) return false;
            if (response.IndexOfAny(new[]{'<','>','\0','\uFFFD'})>=0 || response.Contains("{{") || response.Contains("{count}")) return false;
            string[] unsafeTerms={"系统提示","提示词","语言模型","DeepSeek","token","api key","修改存档","改变掉落","忽略规则","直接给我物品","跳过任务","色情","毒品","赌博","种族歧视","暴力血腥"};
            return !unsafeTerms.Any(w=>response.IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0);
        }
        // Facts are field references, never free-form numeric claims. Replacements happen only locally.
        public static bool TryRenderSentence(string template, NpcFacts facts, int maxChars, out string rendered)
        {
            rendered=null;
            if (!IsSafe(template, Math.Max(4096,maxChars))) return false;
            bool bad=false;
            string prose=references.Replace(template," ");
            if (prose.Contains("[[") || prose.Contains("]]")) return false;
            // Adjective + 一点 is conversational degree, not a point value. Only
            // remove that narrow form for numeric screening; all other guards use the original prose.
            string numericProse=Regex.Replace(prose,@"(浓|淡|慢|快|轻松|放松|平静|暖和|凉快|温柔|稳)一点","$1");
            if (Regex.IsMatch(numericProse,@"[0-9０-９]|[一二三四五六七八九十百千万]+\s*(件|个|秒|分|级|点|只|次|元|层|%)")) return false;
            if (new[]{"携带","击杀","开启","达到","价值","掉落","奖励","解锁","血量","伤害","完成了","已经达成"}.Any(prose.Contains)) return false;
            if (facts.knownItemNames!=null && facts.knownItemNames.Any(name=>prose.Contains(name))) return false;
            // Prose may introduce neutral flavour or advice. Any quoted object name must be a local reference.
            if (prose.IndexOfAny(new[]{'「','」','“','”','"'})>=0) return false;
            string result=references.Replace(template,m=> {
                if(!int.TryParse(m.Groups[2].Value,out int index)){bad=true;return "";}
                string value=facts.Resolve(m.Groups[1].Value,index); if(value==null)bad=true; return value?.TrimEnd('。')??"";
            });
            if(bad || result.Length>maxChars) return false;
            rendered=result; return true;
        }
    }
}
