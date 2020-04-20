using System;
public interface IHydratable {
    int Hydration { get; }

    int Hydrate(int max);
}
