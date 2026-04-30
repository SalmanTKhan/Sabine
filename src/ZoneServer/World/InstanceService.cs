using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Sabine.Zone.World
{
	/// <summary>
	/// In-memory registry of "instances" created via @instance. Sabine
	/// does not duplicate maps for instances yet; the entry just records
	/// the request so the command has observable state.
	/// </summary>
	public class InstanceService
	{
		public static InstanceService Instance { get; } = new InstanceService();

		private readonly ConcurrentDictionary<int, GameInstance> _instances = new();
		private int _nextId;

		public GameInstance Create(string name, int ownerCharId)
		{
			var inst = new GameInstance
			{
				Id = Interlocked.Increment(ref _nextId),
				Name = name,
				OwnerCharId = ownerCharId,
			};
			_instances[inst.Id] = inst;
			return inst;
		}

		public bool Destroy(int id) => _instances.TryRemove(id, out _);
		public bool TryGet(int id, out GameInstance inst) => _instances.TryGetValue(id, out inst);
		public IEnumerable<GameInstance> All => _instances.Values;
	}

	public class GameInstance
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int OwnerCharId { get; set; }
	}
}
