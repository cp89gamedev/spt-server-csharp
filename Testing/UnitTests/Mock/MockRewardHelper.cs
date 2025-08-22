using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace UnitTests.Mock;

[Injectable(TypeOverride = typeof(RewardHelper))]
public class MockRewardHelper(
    ISptLogger<RewardHelper> logger,
    TimeUtil timeUtil,
    ItemHelper itemHelper,
    DatabaseService databaseService,
    ProfileHelper profileHelper,
    ServerLocalisationService serverLocalisationService,
    TraderHelper traderHelper,
    PresetHelper presetHelper,
    NotificationSendHelper notificationSendHelper,
    ICloner cloner
) : RewardHelper(logger, timeUtil, itemHelper, databaseService, profileHelper, serverLocalisationService, traderHelper, presetHelper, notificationSendHelper, cloner)
{
    /// <summary>
    /// Mock: Return a predictable list of items based on the input rewards.
    /// If rewards contain item rewards, return those items (ensuring Id/Upd set);
    /// otherwise return a single placeholder item.
    /// </summary>
    public new List<Item> ApplyRewards(
        IEnumerable<Reward> rewards,
        string rewardSource,
        SptProfile fullProfile,
        PmcData profileData,
        MongoId rewardSourceId,
        ItemEventRouterResponse? questResponse = null
    )
    {
        var result =
            rewards
                ?.Where(r => r?.Type == RewardType.Item && r.Items is not null)
                .SelectMany(r => r.Items!)
                .Select(i => new Item
                {
                    Id = i.Id.IsEmpty ? new MongoId() : i.Id,
                    Template = i.Template,
                    ParentId = i.ParentId,
                    SlotId = i.SlotId,
                    Location = i.Location,
                    Desc = i.Desc,
                    Upd = i.Upd ?? new Upd { StackObjectsCount = 1, SpawnedInSession = true },
                })
                .ToList() ?? [];

        if (result.Count == 0)
        {
            result.Add(
                new Item
                {
                    Id = new MongoId(),
                    Template = new MongoId(),
                    Upd = new Upd { StackObjectsCount = 1, SpawnedInSession = true },
                }
            );
        }

        return result;
    }

    /// <summary>
    /// Mock: Always return true (all rewards are considered valid for the game edition).
    /// </summary>
    public new bool RewardIsForGameEdition(Reward reward, string gameVersion)
    {
        return true;
    }

    /// <summary>
    /// Mock: Return a single dummy HideoutProduction that references the provided questId.
    /// </summary>
    public new List<HideoutProduction> GetRewardProductionMatch(Reward craftUnlockReward, MongoId questId)
    {
        return new List<HideoutProduction>
        {
            new()
            {
                Id = new MongoId(),
                AreaType = HideoutAreas.Workbench,
                EndProduct = new MongoId(),
                Requirements = new List<Requirement>
                {
                    new()
                    {
                        Type = "QuestComplete",
                        QuestId = questId,
                        RequiredLevel = craftUnlockReward?.LoyaltyLevel,
                    },
                },
            },
        };
    }

    /// <summary>
    /// Mock: No-op with minimal side effect — ensure the achievements dictionary exists and add the id with a timestamp of 0.
    /// </summary>
    public new void AddAchievementToProfile(SptProfile fullProfile, MongoId achievementId)
    {
        try
        {
            fullProfile?.CharacterData?.PmcData?.Achievements?.TryAdd(achievementId, 0);
        }
        catch
        {
            // Swallow any issues — this is a mock.
        }
    }
}
