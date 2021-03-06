﻿using System;

namespace Drexel.Terminal.Layout
{
    /// <summary>
    /// Represents a moveable two-dimensional region.
    /// </summary>
    public interface IMoveOnlyRegion : IReadOnlyRegion
    {
        /// <summary>
        /// Gets an observable that occurs when a change to this region is requested.
        /// </summary>
        IObservable<RegionChangeEventArgs> OnChangeRequested { get; }

        /// <summary>
        /// Returns a value indicating whether this region can be moved such that the top-left of this region is equal
        /// to <paramref name="newTopLeft"/>.
        /// </summary>
        /// <param name="newTopLeft">
        /// The new top-left to check against.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if this region can be moved such that its top-left is equal to
        /// <paramref name="newTopLeft"/>; otherwise, <see langword="false"/>.
        /// </returns>
        bool CanMoveTo(Coord newTopLeft);

        /// <summary>
        /// Tries to move this region such that the top-left of this region is equal to <paramref name="newTopLeft"/>,
        /// and returns a value indicating whether the move was successful. If the move was successful,
        /// <see langword="true"/> will be returnend, and <paramref name="beforeChange"/> will be set to a region
        /// equivalent to this region before the move was applied.
        /// </summary>
        /// <param name="newTopLeft">
        /// The coordinate that should become the new top-left coordinate of this region.
        /// </param>
        /// <param name="beforeChange">
        /// If the move succeeds, a region equivalent to this region before the move was applied.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the move succeeded; otherwise, <see langword="false"/>.
        /// </returns>
        bool TryMoveTo(Coord newTopLeft, out IReadOnlyRegion beforeChange);
    }
}
