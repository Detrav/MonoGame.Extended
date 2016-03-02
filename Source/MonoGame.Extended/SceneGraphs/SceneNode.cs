﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Shapes;

namespace MonoGame.Extended.SceneGraphs
{
    public class SceneNode : IMovable, IRotatable, IScalable
    {
        private SceneNode(string name, SceneNode parent, Vector2 position, float rotation, Vector2 scale)
        {
            Name = name;
            Parent = parent;
            Position = position;
            Rotation = rotation;
            Scale = scale;

            _children = new List<SceneNode>();
            _entities = new List<ISceneEntity>();
        }

        private readonly List<SceneNode> _children;
        private readonly List<ISceneEntity> _entities;

        public string Name { get; set; }
        public Vector2 Position { get; set; }
        public float Rotation { get; set; }
        public Vector2 Scale { get; set; }
        public SceneNode Parent { get; private set; }
        public IEnumerable<SceneNode> Children => _children;
        public IEnumerable<ISceneEntity> Entities => _entities;
        public object Tag { get; set; }

        public RectangleF GetBoundingRectangle()
        {
            Vector2 position, scale;
            float rotation;
            GetWorldTransform().Decompose(out position, out rotation, out scale);

            var rectangles = _entities
                .Select(e =>
                {
                    var r = e.GetBoundingRectangle();
                    r.Offset(position);
                    return r;
                })
                .Concat(_children.Select(i => i.GetBoundingRectangle()))
                .ToArray();
            var x0 = rectangles.Min(r => r.Left);
            var y0 = rectangles.Min(r => r.Top);
            var x1 = rectangles.Max(r => r.Right);
            var y1 = rectangles.Max(r => r.Bottom);

            
            return new RectangleF(x0, y0, x1 - x0, y1 - y0);
        }
        
        internal static SceneNode CreateRootNode()
        {
            return new SceneNode(null, null, Vector2.Zero, 0, Vector2.One);
        }

        public SceneNode CreateChildSceneNode(string name, Vector2 position, float rotation, Vector2 scale)
        {
            var sceneNode = new SceneNode(name, this, position, rotation, scale);
            _children.Add(sceneNode);
            return sceneNode;
        }

        public SceneNode CreateChildSceneNode(string name, Vector2 position, float rotation)
        {
            return CreateChildSceneNode(name, position, rotation, Vector2.One);
        }

        public SceneNode CreateChildSceneNode(string name, Vector2 position)
        {
            return CreateChildSceneNode(name, position, 0, Vector2.One);
        }

        public SceneNode CreateChildSceneNode(string name)
        {
            return CreateChildSceneNode(name, Vector2.Zero, 0, Vector2.One);
        }

        public SceneNode CreateChildSceneNode()
        {
            return CreateChildSceneNode(null, Vector2.Zero, 0, Vector2.One);
        }

        public SceneNode CreateChildSceneNode(Vector2 position, float rotation, Vector2 scale)
        {
            return CreateChildSceneNode(null, position, rotation, scale);
        }

        public SceneNode CreateChildSceneNode(Vector2 position, float rotation)
        {
            return CreateChildSceneNode(null, position, rotation, Vector2.One);
        }

        public SceneNode CreateChildSceneNode(Vector2 position)
        {
            return CreateChildSceneNode(null, position, 0, Vector2.One);
        }

        public void RemoveChildSceneNode(int index)
        {
            RemoveChildSceneNode(_children[index]);
        }

        public void RemoveChildSceneNode(SceneNode sceneNode)
        {
            if (sceneNode.Parent != this)
                throw new InvalidOperationException($"{sceneNode} does not belong to parent");

            sceneNode.Parent = null;
            _children.Remove(sceneNode);
        }

        public void Attach(ISceneEntity entity)
        {
            _entities.Add(entity);
        }

        public Matrix GetWorldTransform()
        {
            return Parent == null ? Matrix.Identity : Matrix.Multiply(GetLocalTransform(), Parent.GetWorldTransform());
        }

        public Matrix GetLocalTransform()
        {
            var rotationMatrix = Matrix.CreateRotationZ(Rotation);
            var scaleMatrix = Matrix.CreateScale(new Vector3(Scale.X, Scale.Y, 1));
            var translationMatrix = Matrix.CreateTranslation(new Vector3(Position.X, Position.Y, 0));
            var tempMatrix = Matrix.Multiply(scaleMatrix, rotationMatrix);
            return Matrix.Multiply(tempMatrix, translationMatrix);
        }

        public override string ToString()
        {
            return $"name: {Name}, position: {Position}, rotation: {Rotation}, scale: {Scale}";
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Vector2 offsetPosition, offsetScale;
            float offsetRotation;
            var worldTransform = GetWorldTransform();
            worldTransform.Decompose(out offsetPosition, out offsetRotation, out offsetScale);

            foreach (var drawable in Entities.OfType<ISpriteBatchDrawable>())
            {
                if (drawable.IsVisible)
                {
                    var texture = drawable.TextureRegion.Texture;
                    var sourceRectangle = drawable.TextureRegion.Bounds;
                    var position = offsetPosition + drawable.Position;
                    var rotation = offsetRotation + drawable.Rotation;
                    var scale = offsetScale * drawable.Scale;

                    spriteBatch.Draw(texture, position, sourceRectangle, drawable.Color, rotation, drawable.Origin, scale, drawable.Effect, 0);
                }
            }

            foreach (var child in Children)
                child.Draw(spriteBatch);
        }
    }
}