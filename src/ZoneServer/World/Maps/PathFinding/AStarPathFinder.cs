using System;
using System.Collections.Generic;
using Sabine.Shared.Data.Databases;
using Sabine.Shared.World;

namespace Sabine.Zone.World.Maps.PathFinding
{
	/// <summary>
	/// Optimized A* pathfinding implementation with proper heap and hash-based lookups.
	/// </summary>
	public class AStarPathFinder : IPathFinder
	{
		private readonly MapCacheData _mapCacheData;
		private const int MaxPathLength = 32;
		private const int MoveCost = 10;
		private const int MoveDiagonalCost = 14;

		// Reusable structures to avoid allocations
		private readonly MinHeap<PathNode> _openSet;
		private readonly Dictionary<long, PathNode> _nodeCache;
		private readonly List<Position> _pathBuffer;

		public AStarPathFinder(MapCacheData mapCacheData)
		{
			_mapCacheData = mapCacheData;
			_openSet = new MinHeap<PathNode>(256);
			_nodeCache = new Dictionary<long, PathNode>(256);
			_pathBuffer = new List<Position>(MaxPathLength);
		}

		/// <summary>
		/// Finds a path between the given positions and returns it.
		/// </summary>
		public Position[] FindPath(Position from, Position to)
		{
			if (!ValidatePositions(from, to, out var quickResult))
				return quickResult;

			// Try simple path first (cheaper)
			if (TrySimplePath(from, to, out var simplePath))
				return simplePath;

			// Fall back to full A*
			return FindPathAStar(from, to);
		}

		/// <summary>
		/// Returns true if there's a valid path to get from one position to the other.
		/// </summary>
		public bool PathExists(Position from, Position to)
		{
			if (!ValidatePositions(from, to, out _))
				return false;

			// Quick check: try simple path first
			if (TrySimplePath(from, to, out _))
				return true;

			// Do full A* search
			var path = FindPathAStar(from, to);
			return path != null && path.Length > 0;
		}

		/// <summary>
		/// Validates start and end positions, returns false if invalid.
		/// </summary>
		private bool ValidatePositions(Position from, Position to, out Position[] result)
		{
			result = Array.Empty<Position>();

			// Validate bounds
			if (from.X < 0 || from.X >= _mapCacheData.Width || from.Y < 0 || from.Y >= _mapCacheData.Height)
				return false;

			if (to.X < 0 || to.X >= _mapCacheData.Width || to.Y < 0 || to.Y >= _mapCacheData.Height)
				return false;

			// Check if destination is walkable
			if (_mapCacheData.IsUnpassable(to.X, to.Y))
				return false;

			// Already at destination
			if (from.X == to.X && from.Y == to.Y)
			{
				result = Array.Empty<Position>();
				return false;
			}

			return true;
		}

		/// <summary>
		/// Tries to find a simple direct path (diagonal then straight).
		/// Much faster than A* for unobstructed paths.
		/// </summary>
		private bool TrySimplePath(Position from, Position to, out Position[] path)
		{
			_pathBuffer.Clear();

			var dx = Math.Sign(to.X - from.X);
			var dy = Math.Sign(to.Y - from.Y);
			var x = from.X;
			var y = from.Y;

			while ((x != to.X || y != to.Y) && _pathBuffer.Count < MaxPathLength)
			{
				x += dx;
				y += dy;

				if (x == to.X) dx = 0;
				if (y == to.Y) dy = 0;

				// Check if this cell is walkable
				if (_mapCacheData.IsUnpassable(x, y))
				{
					path = null;
					return false;
				}

				_pathBuffer.Add(new Position(x, y));
			}

			if (x == to.X && y == to.Y)
			{
				path = _pathBuffer.ToArray();
				return true;
			}

			path = null;
			return false;
		}

		/// <summary>
		/// Full A* pathfinding with optimized data structures.
		/// </summary>
		private Position[] FindPathAStar(Position from, Position to)
		{
			_openSet.Clear();
			_nodeCache.Clear();

			var startNode = new PathNode(from.X, from.Y, 0, Heuristic(from, to), null);
			_openSet.Push(startNode);
			_nodeCache[GetKey(from.X, from.Y)] = startNode;

			PathNode goalNode = null;

			while (_openSet.Count > 0)
			{
				var current = _openSet.Pop();

				// Reached goal
				if (current.X == to.X && current.Y == to.Y)
				{
					goalNode = current;
					break;
				}

				// Mark as closed
				current.Closed = true;

				// Explore neighbors
				ExploreNeighbors(current, to);
			}

			// Reconstruct path
			if (goalNode == null)
				return Array.Empty<Position>();

			return ReconstructPath(goalNode);
		}

		/// <summary>
		/// Explores all valid neighbors of the current node.
		/// </summary>
		private void ExploreNeighbors(PathNode current, Position goal)
		{
			var x = current.X;
			var y = current.Y;

			// Check which cardinal directions are passable
			var canNorth = y < _mapCacheData.Height - 1 && !_mapCacheData.IsUnpassable(x, y + 1);
			var canSouth = y > 0 && !_mapCacheData.IsUnpassable(x, y - 1);
			var canEast = x < _mapCacheData.Width - 1 && !_mapCacheData.IsUnpassable(x + 1, y);
			var canWest = x > 0 && !_mapCacheData.IsUnpassable(x - 1, y);

			// Cardinal directions
			if (canNorth) ProcessNeighbor(current, x, y + 1, MoveCost, goal);
			if (canSouth) ProcessNeighbor(current, x, y - 1, MoveCost, goal);
			if (canEast) ProcessNeighbor(current, x + 1, y, MoveCost, goal);
			if (canWest) ProcessNeighbor(current, x - 1, y, MoveCost, goal);

			// Diagonal directions (only if both adjacent cardinals are passable)
			if (canNorth && canEast && !_mapCacheData.IsUnpassable(x + 1, y + 1))
				ProcessNeighbor(current, x + 1, y + 1, MoveDiagonalCost, goal);

			if (canNorth && canWest && !_mapCacheData.IsUnpassable(x - 1, y + 1))
				ProcessNeighbor(current, x - 1, y + 1, MoveDiagonalCost, goal);

			if (canSouth && canEast && !_mapCacheData.IsUnpassable(x + 1, y - 1))
				ProcessNeighbor(current, x + 1, y - 1, MoveDiagonalCost, goal);

			if (canSouth && canWest && !_mapCacheData.IsUnpassable(x - 1, y - 1))
				ProcessNeighbor(current, x - 1, y - 1, MoveDiagonalCost, goal);
		}

		/// <summary>
		/// Processes a single neighbor node.
		/// </summary>
		private void ProcessNeighbor(PathNode current, int x, int y, int moveCost, Position goal)
		{
			var key = GetKey(x, y);
			var newGCost = current.GCost + moveCost;

			if (_nodeCache.TryGetValue(key, out var existingNode))
			{
				// Already processed and closed
				if (existingNode.Closed)
					return;

				// Found better path to this node
				if (newGCost < existingNode.GCost)
				{
					existingNode.GCost = newGCost;
					existingNode.FCost = newGCost + existingNode.HCost;
					existingNode.Parent = current;
					_openSet.UpdatePriority(existingNode);
				}
			}
			else
			{
				// New node
				var hCost = Heuristic(x, y, goal.X, goal.Y);
				var newNode = new PathNode(x, y, newGCost, hCost, current);
				_nodeCache[key] = newNode;
				_openSet.Push(newNode);
			}
		}

		/// <summary>
		/// Reconstructs the path from goal to start.
		/// </summary>
		private Position[] ReconstructPath(PathNode goalNode)
		{
			_pathBuffer.Clear();

			var current = goalNode;
			while (current.Parent != null)
			{
				_pathBuffer.Add(new Position(current.X, current.Y));
				current = current.Parent;
			}

			_pathBuffer.Reverse();
			return _pathBuffer.ToArray();
		}

		/// <summary>
		/// Manhattan distance heuristic.
		/// </summary>
		private static int Heuristic(Position from, Position to)
			=> Heuristic(from.X, from.Y, to.X, to.Y);

		private static int Heuristic(int x0, int y0, int x1, int y1)
			=> MoveCost * (Math.Abs(x1 - x0) + Math.Abs(y1 - y0));

		/// <summary>
		/// Creates a unique key for a position.
		/// </summary>
		private static long GetKey(int x, int y)
			=> ((long)x << 32) | (long)(uint)y;

		/// <summary>
		/// Represents a node in the pathfinding graph.
		/// </summary>
		private class PathNode : IComparable<PathNode>
		{
			public int X;
			public int Y;
			public int GCost; // Cost from start
			public int HCost; // Heuristic to goal
			public int FCost; // Total cost (G + H)
			public PathNode Parent;
			public bool Closed;

			public PathNode(int x, int y, int gCost, int hCost, PathNode parent)
			{
				X = x;
				Y = y;
				GCost = gCost;
				HCost = hCost;
				FCost = gCost + hCost;
				Parent = parent;
				Closed = false;
			}

			public int CompareTo(PathNode other)
				=> FCost.CompareTo(other.FCost);
		}

		/// <summary>
		/// Simple binary min-heap for efficient priority queue operations.
		/// </summary>
		private class MinHeap<T> where T : IComparable<T>
		{
			private T[] _items;
			private int _count;

			public int Count => _count;

			public MinHeap(int capacity)
			{
				_items = new T[capacity];
				_count = 0;
			}

			public void Clear()
			{
				_count = 0;
			}

			public void Push(T item)
			{
				if (_count == _items.Length)
					Array.Resize(ref _items, _items.Length * 2);

				_items[_count] = item;
				HeapifyUp(_count);
				_count++;
			}

			public T Pop()
			{
				if (_count == 0)
					throw new InvalidOperationException("Heap is empty");

				var result = _items[0];
				_count--;

				if (_count > 0)
				{
					_items[0] = _items[_count];
					HeapifyDown(0);
				}

				return result;
			}

			public void UpdatePriority(T item)
			{
				// Find the item and bubble it up if needed
				for (var i = 0; i < _count; i++)
				{
					if (ReferenceEquals(_items[i], item))
					{
						HeapifyUp(i);
						return;
					}
				}
			}

			private void HeapifyUp(int index)
			{
				while (index > 0)
				{
					var parentIndex = (index - 1) / 2;

					if (_items[index].CompareTo(_items[parentIndex]) >= 0)
						break;

					Swap(index, parentIndex);
					index = parentIndex;
				}
			}

			private void HeapifyDown(int index)
			{
				while (true)
				{
					var smallest = index;
					var leftChild = 2 * index + 1;
					var rightChild = 2 * index + 2;

					if (leftChild < _count && _items[leftChild].CompareTo(_items[smallest]) < 0)
						smallest = leftChild;

					if (rightChild < _count && _items[rightChild].CompareTo(_items[smallest]) < 0)
						smallest = rightChild;

					if (smallest == index)
						break;

					Swap(index, smallest);
					index = smallest;
				}
			}

			private void Swap(int i, int j)
			{
				var temp = _items[i];
				_items[i] = _items[j];
				_items[j] = temp;
			}
		}
	}
}
