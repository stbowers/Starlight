using System.Collections.Generic;
using StarlightEngine.Graphics.Objects;

namespace StarlightEngine.Graphics.Scenes
{
	public class Scene
	{
		SortedDictionary<int, List<IGraphicsObject>> m_objects = new SortedDictionary<int, List<IGraphicsObject>>();

		public Scene()
		{
		}

		public void AddObject(int layer, IGraphicsObject obj)
		{
			List<IGraphicsObject> list;
			if (m_objects.ContainsKey(layer))
			{
				list = m_objects[layer];
			}
			else
			{
				list = new List<IGraphicsObject>();
				m_objects.Add(layer, list);
			}

			list.Add(obj);

			// If it's a collection, add the other objects too
			if (obj is ICollectionObject)
			{
				foreach (IGraphicsObject child in (obj as ICollectionObject).Objects)
				{
					AddObject(layer, child);
				}
			}
		}

		public void RemoveObject(IGraphicsObject obj)
		{
			foreach (var objList in m_objects)
			{
				if (objList.Value.Contains(obj))
				{
					objList.Value.Remove(obj);
				}
			}
		}

		public SortedDictionary<int, List<IGraphicsObject>> GetObjects()
		{
			return m_objects;
		}
	}
}
