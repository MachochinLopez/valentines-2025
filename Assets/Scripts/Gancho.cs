using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Gancho : MonoBehaviour
{
    /***************
     *** Prefabs ***
     ***************/

    public GameObject threadPrefab;

    /*********************************
     *** Referencias a los objetos ***
     *********************************/

    public RectTransform yarn;
    private RectTransform hook;

    /***********************************
     *** Referencias al input system ***
     ***********************************/

    private InputSystem_Actions inputActions;

    /************************************
     *** Variables para generar hilos ***
     ************************************/

    /// <summary>
    /// Indica si el gancho ha recogido el estambre. Se activa cuando el gancho
    /// se superpone con el estambre y permite iniciar la generación de
    /// segmentos de hilo.
    /// </summary>
    private bool hasPickedYarn = false;

    /// <summary>
    /// Longitud de cada segmento de hilo.
    /// Luego de superar esta distancia, se generará un nuevo segmento de hilo.
    /// </summary>
    public float segmentLength = 1f;

    /// <summary>
    /// Radio máximo de generación de hilo.
    /// </summary>
    public float maxRadius = 10f;

    private readonly List<GameObject> segments = new();
    private Vector3 lastYarnSegmentPosition;
    private HingeJoint2D hookHinge;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
    }

    private void Start()
    {
        hook = GetComponent<RectTransform>();
        hookHinge = GetComponent<HingeJoint2D>();
        lastYarnSegmentPosition = yarn.position;
    }

    private void OnEnable()
    {
        inputActions.UI.Point.performed += MoveCursor;
        inputActions.UI.Point.performed += SpawnYarn;
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.UI.Point.performed -= MoveCursor;
        inputActions.UI.Point.performed -= SpawnYarn;
        inputActions.Disable();
    }

    /// <summary>
    /// Mueve el cursor a la posición del puntero.
    /// </summary>
    /// <param name="context">Contexto del input.</param>
    private void MoveCursor(InputAction.CallbackContext context)
    {
        Vector2 pointerPosition = context.ReadValue<Vector2>();
        Rigidbody2D ganchoRB = GetComponent<Rigidbody2D>();

        ganchoRB.MovePosition(
            Camera.main.ScreenToWorldPoint(new Vector3(pointerPosition.x, pointerPosition.y, 0))
        );

        if (!hasPickedYarn && IsOverlapping(yarn, hook))
        {
            hasPickedYarn = true;
        }
    }

    /// <summary>
    /// Genera un nuevo segmento de hilo cada vez que el gancho se aleja del estambre
    /// una distancia mayor a la longitud de un segmento.
    /// </summary>
    private void SpawnYarn(InputAction.CallbackContext context)
    {
        if (!hasPickedYarn)
            return;

        float currentThreadLength = segments.Count * segmentLength;
        float distanceToYarn = Vector3.Distance(hook.position, yarn.position);
        if (distanceToYarn <= currentThreadLength)
            return;

        if (distanceToYarn > maxRadius)
            return;

        // Calculate rotation to point towards the hook
        Vector3 direction = (hook.position - lastYarnSegmentPosition).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // Subtract 90 to align sprite's up with direction
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        GameObject newSegment = Instantiate(threadPrefab, lastYarnSegmentPosition, rotation, yarn);

        HingeJoint2D newJoint = newSegment.GetComponent<HingeJoint2D>();

        segments.Add(newSegment);

        if (segments.Count > 1)
            newJoint.connectedBody = segments[^2].GetComponent<Rigidbody2D>();
        else
            newJoint.connectedBody = yarn.GetComponent<Rigidbody2D>();

        hookHinge.connectedBody = newJoint.GetComponent<Rigidbody2D>();
        lastYarnSegmentPosition = hook.position;
    }

    /// <summary>
    /// Verifica si dos rectángulos se superponen.
    /// </summary>
    /// <param name="rect1">Primer rectángulo.</param>
    /// <param name="rect2">Segundo rectángulo.</param>
    /// <returns>True si los rectángulos se superponen, false en caso contrario.</returns>
    private bool IsOverlapping(RectTransform rect1, RectTransform rect2)
    {
        Rect r1 = GetWorldRect(rect1);
        Rect r2 = GetWorldRect(rect2);
        return r1.Overlaps(r2);
    }

    /// <summary>
    /// Obtiene el rectángulo en coordenadas del mundo.
    /// </summary>
    /// <param name="rectTransform">Rectángulo a convertir.</param>
    /// <returns>Rectángulo en coordenadas del mundo.</returns>
    private Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        return new Rect(
            corners[0].x,
            corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y
        );
    }
}
