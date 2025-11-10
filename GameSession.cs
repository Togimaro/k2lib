
namespace LouveSystems.K2.Lib
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class GameSession
    {
        public event Action<Transform> OnTransformAdded;

        public ManagedRandom Random { get; }

        public GameRules Rules => parameters;

        public GameState CurrentGameState => gameState;

        public IReadOnlyList<Transform> AwaitingTransforms => awaitingTransforms;

        public IReadOnlyDictionary<byte, SessionPlayer> SessionPlayers => sessionPlayers;

        private readonly List<Transform> awaitingTransforms = new List<Transform>();

        private readonly GameRules parameters;

        private readonly Dictionary<byte, SessionPlayer> sessionPlayers = new Dictionary<byte, SessionPlayer>();

        protected GameState gameState;


        public GameSession(PartySessionInitializationParameters party, GameRules parameters, int seed)
        {
            this.parameters = parameters;

            this.Random = new ManagedRandom(seed);

            gameState = new GameState(party, parameters);

            List<byte> realmsIndices = new List<byte>();
            for (byte i = 0; i < gameState.world.Realms.Count; i++) {
                realmsIndices.Add(i);
            }

            realmsIndices.RemoveAll(CurrentGameState.world.IsRealmExcludedFromVoting);
            realmsIndices.Shuffle(Random);

            gameState.world.Modify(out Region[] regions, out Realm[] realms);

            for (byte i = 0; i < party.realmsToInitialize.Length; i++) {
                byte playerId = party.realmsToInitialize[i].forPlayerId;
                sessionPlayers[playerId] = CreatePlayer(playerId);
                sessionPlayers[playerId].RealmIndex = realmsIndices[i];
                realms[realmsIndices[i]].factionIndex = party.realmsToInitialize[i].factionIndex;
            }


            gameState.daysRemainingBeforeNextCouncil = (byte)(parameters.turnsBetweenVotes + parameters.initialVoteTurnsDelay);
        }

        public bool GetPlannedConstructionForRegion(int regionIndex, out EBuilding building)
        {
            foreach (var player in SessionPlayers) {
                if (player.Value.IsBuildingSomething(regionIndex, out building)) {
                    return true;
                }
            }

            building = default;
            return false;
        }

        public bool EverybodyHasPlayed()
        {
            foreach (var player in SessionPlayers) {
                if (player.Value.AnyDecisionsRemaining() || player.Value.CanUpgradeAdministration()) {
                    return false;
                }
            }

            return true;
        }

        protected virtual SessionPlayer CreatePlayer(byte playerId)
        {
            return new SessionPlayer(this);
        }

        public virtual bool AddTransform(Transform transform)
        {
            return AddTransformIfPossible(transform);
        }

        public bool IsFavoured(byte realmIndex)
        {
            return gameState.world.Realms[realmIndex].isFavoured;
        }

        public bool HasRegionPlayed(int regionIndex)
        {
            for (int i = 0; i < awaitingTransforms.Count; i++) {
                if (awaitingTransforms[i] is RegionRelatedTransform attack && attack.actingRegionIndex == regionIndex) {
                    return true;
                }
            }

            return false;
        }

        // Returns TRUE if the game can continue
        // Returns FALSE if the game has ended
        public bool Advance()
        {
            ResolveTransformsToEffects(out ITransformEffect[] orderedEffects);
            GameState newGameState = gameState.Duplicate();
            gameState.ApplyEffects(orderedEffects, ref newGameState);

            newGameState.world.Modify(out Region[] regions, out Realm[] realms);

            // Resolve revenue
            for (int regionIndex = 0; regionIndex < newGameState.world.Regions.Count; regionIndex++) {
                if (newGameState.world.Regions[regionIndex].GetOwner(out byte owningRealm)) {
                    int revenue = newGameState.world.GetRegionSilverWorth(regionIndex);
                    newGameState.world.AddSilverTreasury(owningRealm, revenue);
                }
            }

            gameState = newGameState;

            AdvanceDay();

            if (gameState.daysRemainingBeforeNextCouncil <= 0) {
                gameState.voting.ComputeVotes(in gameState, Random);
                AdvanceAfterCouncil();


                if (gameState.voting.Result.HasMajorityWinner(out byte winnerIndex)) {
                    return false;
                }
            }

            return true;
        }

        protected void ResolveTransformsToEffects(out ITransformEffect[] effects)
        {
            effects = new ITransformEffect[0];
            try {
                CurrentGameState.ComputeEffects(Random, AwaitingTransforms, out effects);
            }
            catch (Exception e) {
                Logger.Error(e);
            }

            awaitingTransforms.Clear();

        }
        protected void AdvanceDay()
        {
            gameState.daysRemainingBeforeNextCouncil--;

            gameState.daysPassed++;
        }

        protected void AdvanceAfterCouncil()
        {
            gameState.daysRemainingBeforeNextCouncil = this.Rules.turnsBetweenVotes;

            gameState.councilsPassed++;

            // Reset favours
            for (int realmIndex = 0; realmIndex < gameState.world.Realms.Count; realmIndex++) {
                gameState.world.Modify(out Region[] regions, out Realm[] realms);
                realms[realmIndex].isFavoured = false;
            }
        }


        private bool AddTransformIfPossible(Transform transform)
        {
            if (transform is RegionRelatedTransform regionTransform &&
                HasRegionPlayed(regionTransform.actingRegionIndex) &&
                !gameState.world.Regions[regionTransform.actingRegionIndex].CanReplay(Rules) &&
                !(regionTransform is RegionAttackRegionTransform atk && atk.isExtendedAttack)) {
                Logger.Warn($"Discarding transform {transform} because this region cannot replay!");
                return false;
            }

            if (transform.CompatibleWith(AwaitingTransforms)) {
                Logger.Info($"Adding transform {transform}");
                awaitingTransforms.Add(transform);
                OnTransformAdded?.Invoke(transform);
                return true;
            }
            else {
                Logger.Warn($"Discarding transform {transform} because it is not compatible with current awaiting transforms");
                return false;
            }
        }
    }
}