// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
//
// namespace AsiTimeLine.Editor
// {
//     public class ComponentModificationProcessor : AssetModificationProcessor
//     {
//         private static HashSet<GameObject> processedObjects = new HashSet<GameObject>();
//
//         private static void OnWillSaveAssets(string[] paths)
//         {
//             foreach (string path in paths)
//             {
//                 GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
//                 if (obj != null)
//                 {
//                     // if (!processedObjects.Contains(obj))
//                     // {
//                     //     MyComponent myComponent = obj.GetComponent<MyComponent>();
//                     //     if (myComponent != null)
//                     //     {
//                     //         Debug.Log("组件被添加，执行自定义初始化");
//                     //         // 在这里添加你希望在添加组件时执行的代码
//                     //         myComponent.ExecuteOnAdd();
//                     //         processedObjects.Add(obj);/
//                     //     }
//                     // }
//                 }
//             }
//         }
//     }
// }