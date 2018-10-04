using System.Collections.Generic;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Events;

namespace StarlightEngine.Graphics.Scenes
{
    public class Scene
    {
        SortedDictionary<int, List<IGraphicsObject>> m_objects = new SortedDictionary<int, List<IGraphicsObject>>();
        List<(EventManager.HandleEventDelegate, EventType)> m_eventListeners = new List<(EventManager.HandleEventDelegate, EventType)>();

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

			// add object
            list.Add(obj);

			// add listeners
			m_eventListeners.AddRange(obj.EventListeners);

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
					// remove object
                    objList.Value.Remove(obj);

					// remove listeners
					foreach ((EventManager.HandleEventDelegate, EventType) listener in obj.EventListeners){
						m_eventListeners.Remove(listener);
					}
                }
            }
        }

        public SortedDictionary<int, List<IGraphicsObject>> GetObjects()
        {
            return m_objects;
        }

        public List<(EventManager.HandleEventDelegate, EventType)> GetEventListeners()
        {
            return m_eventListeners;
        }
    }
}
