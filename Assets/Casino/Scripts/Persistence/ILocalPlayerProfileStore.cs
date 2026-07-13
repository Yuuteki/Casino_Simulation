using System;
using Casino.Core.Identifiers;

namespace Casino.Persistence
{
    public interface ILocalPlayerProfileStore
    {
        ProfileLoadResult Load(PlayerId playerId);

        void Save(LocalPlayerProfile profile);
    }

    public readonly struct ProfileLoadResult
    {
        private readonly LocalPlayerProfile profile;

        private ProfileLoadResult(bool found, LocalPlayerProfile profile)
        {
            if (found && profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            Found = found;
            this.profile = profile;
        }

        public bool Found { get; }

        public LocalPlayerProfile Profile
        {
            get
            {
                if (!Found)
                {
                    throw new InvalidOperationException("No local player profile was loaded.");
                }

                return profile;
            }
        }

        public static ProfileLoadResult Missing()
        {
            return new ProfileLoadResult(false, null);
        }

        public static ProfileLoadResult FoundProfile(LocalPlayerProfile profile)
        {
            return new ProfileLoadResult(true, profile);
        }
    }
}
