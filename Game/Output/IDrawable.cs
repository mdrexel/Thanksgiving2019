﻿namespace Game.Output
{
    public interface IDrawable
    {
        void Draw(ISink sink);

        void Draw(ISink sink, Rectangle region);

        void InvertColor();
    }
}
