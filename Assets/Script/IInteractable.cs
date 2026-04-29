using UnityEngine;

public interface IInteractable
{
    // 多加了一個 Transform 參數，讓被互動的物件知道「是誰」點了它
    void OnInteract(Transform interactor);
}