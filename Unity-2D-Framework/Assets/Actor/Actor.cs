using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum Touchable
{
    enabled, disabled, childrenOnly, parentOnly
}

/// <summary>
/// Used for UI elements that contain a RectTransform.
/// It's recommendet to keep pivots and anchors the same
/// across every RectTransform to avoid headaches
/// This Component allows to conveniently add & remove Actions
/// i.e. for animation or placements. Useful premade actions for animations are provided
/// in Action.class.
/// 
/// Explenation for what RectTransform.pivot is:
/// When the RectTransform is drawn this is the point by which
/// the left bottom corner is pivoted to the left & bottom. It's also used
/// as the point to scale and rotate the RectTransform around. This pivotable
/// point is also called the origin of the RectTransform
/// 
/// 
/// Explenation for what RectTransform.anchorMin/Max is:
/// These points are set in the parents bounds, so that when
/// resizing the parent the childs' bounds resize and reposition with it.
/// These are points inside the parents bounds that move the origin of
/// the parents' coordinate system for the specific child only.
/// Because each child can set these points individually.
/// 
/// Explenation for what RectTransform.anchoredPosition is:
/// It is the position of a RecTransforms origin relative to the parents (0,0) point.
/// (Remember the parents origin is influenced by the anchors of each child individually
/// and the pivot point moves the origin of the childs' RectTransform)
/// 
/// Author: Sebastian Krahnke
/// </summary>
public class Actor : MonoBehaviour, IPointerClickHandler
{
    private List<Action> actions = new List<Action>();
    private List<Action> copy = new List<Action>();

    /// <summary>
    /// currently unused
    /// </summary>
    public Touchable touchable = Touchable.enabled;
    private Vector2 startPos;
    private Vector2 lastScalePointInParentCoord;

    // Start is called before the first frame update
    // Update is called once per frame

    public virtual void Awake()
    {
        // check if RectTransform is present
        if (GetComponent<RectTransform>() == null)
        {
            gameObject.AddComponent<RectTransform>();
        }

        startPos = GetPosition();
    }

    public virtual void Start()
    {
        
    }

    public virtual void Update()
    {
        UpdateActions();
    }
    
    public void UpdateActions()
    {
        copy.AddRange(actions);
        float delta = Time.deltaTime;
        copy.ForEach(action =>
        {
            if (action.Act(delta, this))
            {
                actions.Remove(action);
            }
        });

        copy.Clear();
    }

    // -- parent --

    public RectTransform GetParentRectTransform()
    {
        return GetComponentInParent<RectTransform>();
    }


    // -- color  --

    /// <summary>
    /// Sets the color of TMPro, Image or Canvas Group if present
    /// </summary>
    /// <param name="color"></param>
    public void SetColor(Color color)
    {
        Image img = GetComponent<Image>();
        TextMeshProUGUI tmpGUI = GetComponent<TextMeshProUGUI>();
        //parent 
        if (img != null) // image
        {
            img.color = color;
        }
        if (tmpGUI != null)//tmpro
        {
            tmpGUI.color = color;
        }
        // children
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = color.a;
        }
    }

    /// <summary>
    /// Sets the alpha of TMPro, Image or Canvas Group if present
    /// </summary>
    /// <param name="alpha"></param>
    public void SetAlpha(float alpha)
    {
        Image img = GetComponent<Image>();
        TextMeshProUGUI tmpGUI = GetComponent<TextMeshProUGUI>();
        //parent 
        if (img != null) // image
        {
            Color c = img.color;
            img.color = new Color(c.r, c.g, c.b, alpha);
        }
        if (tmpGUI != null)//tmpro
        {
            Color c = tmpGUI.color;
            tmpGUI.color = new Color(c.r, c.g, c.b, alpha);
        }
        // children
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = alpha;
        }

    }


    // -- touchable --

    /// <summary>
    /// true if touchable == enabled or parentOnly
    /// </summary>
    /// <returns></returns>
    public bool IsTouchable()
    {
        return touchable == Touchable.enabled || touchable == Touchable.parentOnly;

    }


    // -- toggle active state --

    /// <summary>
    /// toggles the state of gameObject.activeSelf
    /// </summary>
    /// <param name="active"></param>
    public void ToggleActive()
    {
        SetActive(!IsActive());
    }

    /// <summary>
    /// sets the state of gameObject.activeSelf
    /// </summary>
    /// <param name="active"></param>
    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    /// <summary>
    /// whether this game object is active itself
    /// </summary>
    /// <returns></returns>
    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    /// <summary>
    /// Whether this gameObject has active ancestors or not
    /// </summary>
    /// <returns></returns>
    public bool HasActiveHierarchy()
    {
        return gameObject.activeInHierarchy;
    }



    // -- action --

    public void AddAction(Action action)
   {
        //Debug.Log("Add Action");
        actions.Add(action);
   }
    
    /**returns the last added action*/
    public Action AddAction(params Action[] actions)
    {
        Action last = null;
        if (actions != null)
            for (int i = 0; i < actions.Length; i++)
                if (actions[i] != null)
                {
                    AddAction(actions[i]);
                    last = actions[i];
                }
        return last;
    }

    public void RemoveAction(Action action)
    {
        actions.Remove(action);
    }

    public void RemoveAllActions()
    {
        actions.RemoveRange(0, actions.Count);
    }

    public List<Action> GetActions()
    {
        return actions;
    }



    // -- canvas --



    /// <summary>
    /// Returns the root Canvas (topmost canvas in the hierarchy);
    /// </summary>
    public static Canvas GetCanvas(RectTransform child)
    {
        Canvas canvas = child.GetComponentInParent<Canvas>();
        return canvas? canvas.rootCanvas : null;
    }

    /// <summary>
    /// Returns the root Canvas (topmost canvas in the hierarchy);
    /// </summary>
    public Canvas GetCanvas()
    {
        return GetCanvas(GetRectTransform());
    }

    /// <summary>
    /// returns the RectTransform of the root canvas
    /// </summary>
    /// <param name="child"></param>
    /// <returns></returns>
    public static RectTransform GetCanvasRectTransform(RectTransform child)
    {
        Canvas canvas = GetCanvas(child);
        return canvas.GetComponent<RectTransform>();
    }

    /// <summary>
    /// returns the RectTransform of the root canvas
    /// </summary>
    /// <returns></returns>
    public RectTransform GetCanvasRectTransform()
    {
        return GetCanvasRectTransform(GetRectTransform());
    }

    // -- pivot --

    /// <summary>
    /// returns the pivot in local coordinates (0-size)
    /// </summary>
    /// <returns></returns>
    public Vector2 GetPivotLocalPoint()
    {
        RectTransform rect = GetRectTransform();
        Vector2 pos = rect.anchoredPosition;
        return new Vector2(pos.x * rect.sizeDelta.x, pos.y * rect.sizeDelta.y);
    }

    /// <summary>
    /// returns the pivot in normalized position (0-1)
    /// </summary>
    /// <returns></returns>
    public Vector2 GetPivot()
    {
        return GetRectTransform().pivot;
    }

    public void SetPivot(Vector2 pivot)
    {
        GetRectTransform().pivot = pivot;
    }

    // -- anchors --




    // -- hit --


    /// <summary>
    /// Not done implemented yet!
    /// Returns the topmost child containing the screenPoint
    /// </summary>
    /// <param name="screenPoint"></param>
    /// <param name="root"></param>
    /// <returns></returns>
    public static RectTransform Hit(Vector2 screenPoint, RectTransform root)
    {
        List<RectTransform> children = new List<RectTransform>(root.GetComponentsInChildren<RectTransform>());
        // sort children
        children.Sort((t1, t2) =>
        {
            return t1.GetSiblingIndex().CompareTo(t2.GetSiblingIndex());
        });

        // loop through children
        foreach (RectTransform rect in children)
        {
            if(RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint))
            {
                // check if has actor
                Actor actor = rect.GetComponent<Actor>();
                if(actor != null)
                {
                    // check touchable ?
                }

                return rect;
            }
        }
        return null;

    }




    // -- Point --

    public static Vector2 ScreenToWorldPoint(Vector2 screenPoint, RectTransform rect){
        Canvas canvas = GetCanvas(rect);
        Camera camera = canvas != null? canvas.worldCamera : Camera.main;

        Vector3 anchoredWorldPoint;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screenPoint, camera, out anchoredWorldPoint);
        return anchoredWorldPoint;
    }


    public Vector2 ScreenToWorldPoint(Vector2 screenPoint)
    {
        return ScreenToWorldPoint(screenPoint, GetRectTransform());
    }


    /// <summary>
    /// Calculates the given screen Point into the given Rect Transorms' 
    /// local Point
    /// </summary>
    /// <param name="screenPoint"></param>
    /// <param name="rect"> </param>
    /// <returns></returns>
    public static Vector2 ScreenToLocalPoint(Vector2 screenPoint, RectTransform rect)
    {
        
        Canvas canvas = GetCanvas(rect); // does the canvas need RenderMode.ScreenSpaceCamera ?
        Camera camera = canvas != null? canvas.worldCamera : Camera.main;

        Vector2 anchoredLocalPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, camera, out anchoredLocalPoint);
        return anchoredLocalPoint;
    }

    public Vector2 ScreenToLocalPoint(Vector2 screenPoint)
    {
        return ScreenToLocalPoint(screenPoint, GetRectTransform());
    }


    /// <summary>
    /// Takes into account the rects' anchors
    /// </summary>
    /// <param name="screenPoint"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public Vector2 ScreenToMyParentPoint(Vector2 screenPoint)
    {
        return ScreenToMyParentPoint(screenPoint, GetRectTransform());
    }

    /// <summary>
    /// Takes into account the rects' anchors
    /// </summary>
    /// <param name="screenPoint"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static Vector2 ScreenToMyParentPoint(Vector2 screenPoint, RectTransform rect)
    {
        return LocalToMyParentPoint(ScreenToLocalPoint(screenPoint, rect), rect);
    }




    public static Vector2 LocalToScreenPoint(Vector2 localPoint, RectTransform rect){
        Vector2 worldPoint = LocalToWorldPoint(localPoint, rect);
        return WorldToScreenPoint(worldPoint, rect);
    }

    public Vector2 LocalToScreenPoint(Vector2 localPoint){
        return LocalToScreenPoint(localPoint, GetRectTransform());
    }

    /// <summary>
    /// Calculates the localPoint in rect to a a point in the 
    /// local coordinate system of toRect
    /// </summary>
    /// <param name="localPoint"></param>
    /// <param name="rect"></param>
    /// <param name="toRect"></param>
    /// <returns></returns>
    public static Vector2 LocalToOtherLocalPoint(Vector2 localPoint, RectTransform rect, RectTransform toRect)
    {
        Vector2 worldPoint = LocalToWorldPoint(localPoint, rect);
        return WorldToLocalPoint(worldPoint, toRect);
    }

    /// <summary>
    /// Calculates the localPoint in this RectTransform to a a point in the local coordinate system of toRect
    /// </summary>
    /// <param name="localPoint"></param>
    /// <param name="rect"></param>
    /// <param name="toRect"></param>
    /// <returns></returns>
    public Vector2 LocalToOtherLocalPoint(Vector2 localPoint, RectTransform toRect)
    {
        return LocalToOtherLocalPoint(localPoint, GetRectTransform(), toRect);
    }

    /// <summary>
    /// Takes into account the rects' anchors
    /// </summary>
    /// <param name="localPoint"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static Vector2 LocalToMyParentPoint(Vector2 localPoint, RectTransform rect)
    {
        return GetPosition(Align.origin, rect) + localPoint;
    }

    /// <summary>
    /// Takes into accout this childs'  anchors
    /// </summary>
    /// <param name="localPoint"></param>
    /// <returns></returns>
    public Vector2 LocalToMyParentPoint(Vector2 localPoint)
    {
        return LocalToMyParentPoint(localPoint, GetRectTransform());
    }

    /// <summary>
    /// Does NOT take into account the RectTransform's anchors.
    /// </summary>
    /// <param name="localPoint"></param>
    /// <returns></returns>
    public Vector2 LocalToParentPoint(Vector2 localPoint)
    {
        return LocalToOtherLocalPoint(localPoint, GetRectTransform(), GetParentRectTransform());
    }

    /// <summary>
    /// Does NOT take into account anchors
    /// </summary>
    /// <param name="localPoint"></param>
    /// <returns></returns>
    public Vector2 LocalToCanvasPoint(Vector2 localPoint)
    {
        return LocalToOtherLocalPoint(localPoint, GetCanvasRectTransform());
    }




    /// <summary>
    /// 
    /// </summary>
    /// <param name="localPoint"></param>
    /// <param name="rect">the RectTransform to which the localPoint belongss</param>
    /// <returns></returns>
    public static Vector2 LocalToWorldPoint(Vector2 localPoint, RectTransform rect)
    {
        return rect.TransformPoint(localPoint);
    }

    public Vector2 LocalToWorldPoint(Vector2 localPoint)
    {
        return LocalToWorldPoint(localPoint, GetRectTransform());
    }




    public static Vector2 WorldToLocalPoint(Vector2 worldPoint, RectTransform rect)
    {
        return rect.InverseTransformPoint(worldPoint);
    }

    public Vector2 WorldToLocalPoint(Vector2 worldPoint)
    {
        return WorldToLocalPoint(worldPoint, GetRectTransform());
    }

    /// <summary>
    /// Takes the rects' anchors into account
    /// </summary>
    /// <param name="worldPoint"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public static Vector2 WorldToMyParentPoint(Vector2 worldPoint, RectTransform rect)
    {
        return LocalToMyParentPoint(WorldToLocalPoint(worldPoint, rect), rect);
    }

    /// <summary>
    /// Takes the rects' anchors into account
    /// </summary>
    /// <param name="worldPoint"></param>
    /// <param name="rect"></param>
    /// <returns></returns>
    public Vector2 WorldToMyParentPoint(Vector2 worldPoint)
    {
        return WorldToMyParentPoint(worldPoint, GetRectTransform());
    }



    public static Vector2 WorldToScreenPoint(Vector2 worldPoint, RectTransform rect)
    {
        Camera camera;
        Canvas canvas = GetCanvas(rect);
        if(canvas == null)camera = Camera.main;
        else camera = canvas.worldCamera;

        return RectTransformUtility.WorldToScreenPoint(camera, worldPoint);
    }

    public Vector2 WorldToScreenPoint(Vector2 worldPoint)
    {
        return WorldToScreenPoint(worldPoint, GetRectTransform());
    }



    // -- scale --

    /// <summary>
    /// Returns localScale of this objects' RectTransform
    /// </summary>
    /// <returns></returns>
    public Vector2 GetScale()
    {
        return GetRectTransform().localScale;
    }

    /// <summary>
    /// Sets the localScale of this objects' RectTransform
    /// </summary>
    /// <param name="scale"></param>
    public void SetScale(Vector2 scale)
    {
        GetRectTransform().localScale = scale;
    }

    /// <summary>
    /// Scales around a given point in the local coordinate system, specified by alignment.
    /// Modifies the anchoredPosition as well.
    /// </summary>
    /// <param name="scale"></param>
    /// <param name="alignment"></param>
    public void ScaleAround(Vector2 scale, Align alignment)
    {
        Vector2 localOriginPoint = GetLocalPosition(alignment);
        ScaleAround(scale, localOriginPoint);
    }

    /// <summary>
    /// Scales around a given point in the local coordinate system.
    /// Modifies the anchoredPosition as well. Resets the scale to not be dependent
    /// on the position
    /// </summary>
    /// <param name="scale"></param>
    /// <param name="alignment"></param>
    public void ScaleAround(Vector2 scale, Vector2 localScalePoint)
    {
        Vector2 scalePointInParentCoord = LocalToMyParentPoint(localScalePoint);

        // reset scale to remove dependency of position
        if (lastScalePointInParentCoord != null)
            ScaleAroundDependent(Vector2.one, lastScalePointInParentCoord);

        // scale
        ScaleAroundDependent(scale, scalePointInParentCoord);
        lastScalePointInParentCoord = scalePointInParentCoord;

    }

    /// <summary>
    /// Scales around a given point in the local coordinate system.
    /// Modifies the anchoredPosition as well. Depends on the current position
    /// </summary>
    /// <param name="scale"></param>
    /// <param name="alignment"></param>
    private void ScaleAroundDependent(Vector2 scale, Vector2 scalePointInParentCoord)
    {
        Vector2 startPoint = GetPosition();
        Vector2 startScale = GetScale();
        Vector2 scaleFactor = scale / startScale;
        Vector2 delta = scalePointInParentCoord - startPoint;
        Vector2 scaledDelta = delta * (scaleFactor - Vector2.one);

        SetScale(scale);
        SetPosition(startPoint - scaledDelta);

        //Vector2 diff = scalePointInParentCoord - startPos; // diff between origin(in parent Point) and start pos
        //SetPosition(scalePointInParentCoord - diff * scaleFactor);
    }


    // -- start position --

    /// <summary>
    /// Convenience method to return the anchored position captured at Start().
    /// Start() must have been called first for this value to be set and the
    /// Actor must be active for Start() to fire.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetPositionAtStart()
    {
        return startPos;
    }


    // -- position --

    /// <summary>
    /// Sets the RectTransform.anchoredPosition.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void SetPosition(float x, float y)
    {
        SetPosition(new Vector2(x, y));
    }

    /// <summary>
    /// Sets the RectTransform.anchoredPosition.
    /// </summary>
    /// <param name="pos"></param>
    public void SetPosition(Vector2 pos)
    {
        SetPosition(pos, Align.origin);
    }

    /// <summary>
    /// Sets the RectTransform.anchoredPosition.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="alignment"></param>
    public void SetPosition(float x, float y, Align alignment)
    {
        SetPosition(new Vector2(x, y), alignment);
    }

    /// <summary>
    /// Sets the RectTransform.anchoredPosition.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="alignment"></param>
    public void SetPosition(Vector2 pos, Align alignment)
    {
        SetPosition(pos, alignment, GetRectTransform());
    }

    /// <summary>
    /// Sets the RectTransform.anchoredPosition of the given rect
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="alignment"></param>
    /// <param name="rect"></param>
    public static void SetPosition(Vector2 pos, Align alignment, RectTransform rect)
    {
        SetX(pos.x, alignment, rect);
        SetY(pos.y, alignment, rect);
    }


    /// <summary>
    /// Positions the given rect with its alignment at the position
    /// given by toRect and its toAlignment
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="alignment"></param>
    /// <param name="otherRect"></param>
    /// <param name="otherAlignment"></param>
    public static void SetPosition(Align alignment, RectTransform rect, Align otherAlignment, RectTransform otherRect)
    {
        Canvas canvas = GetCanvas(rect);
        if(canvas != GetCanvas(otherRect))
            throw new System.Exception("Both RectTransforms must have the same root canvas!");
        //RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Vector2 worldPoint = LocalToWorldPoint(GetLocalPosition(otherAlignment, otherRect), otherRect);
        Vector2 parentPoint = LocalToMyParentPoint(WorldToLocalPoint(worldPoint, rect), rect);
        SetPosition(parentPoint, alignment, rect);

        //Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, rect.transform);
        //Bounds otherBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, otherRect.transform);

    }

    /// <summary>
    /// Positions this gameObjects' RectTransform with the given alignment at
    /// the position of toRect with its toAlignment
    /// </summary>
    /// <param name="alignment"></param>
    /// <param name="otherAlignment"></param>
    /// <param name="otherRect"></param>
    public void SetPosition(Align alignment, Align otherAlignment, RectTransform otherRect)
    {
        SetPosition(alignment, GetRectTransform(), otherAlignment, otherRect);
    }


    /// <summary>
    /// Returns the RectTransform.anchoredPosition.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetPosition()
    {
        return GetPosition(Align.origin);
    }

    /// <summary>
    /// returns a new vector
    /// </summary>
    public Vector2 GetPosition(Align alignment)
    {
        return GetPosition(alignment, GetRectTransform());
    }

    public static Vector2 GetPosition(Align alignment, RectTransform rect)
    {
        return new Vector2(GetX(alignment, rect), GetY(alignment, rect));

    }


    /// <summary>
    /// (0,0) is at Align.origin position
    /// </summary>
    public Vector2 GetLocalPosition(Align alignment)
    {
        return GetLocalPosition(alignment, GetRectTransform());
    }

    /// <summary>
    /// (0,0) is at Align.origin position
    /// </summary>
    public static Vector2 GetLocalPosition(Align alignment, RectTransform rect)
    {
        return new Vector2(
            GetX(alignment, rect) - GetX(Align.origin, rect), 
            GetY(alignment, rect) - GetY(Align.origin, rect));
    }




    // x

    public void SetX(float x)
    {
        SetX(x, Align.origin);
    }

    public void SetX(float x, Align alignment)
    {
        SetX(x, alignment, GetRectTransform());
    }

    public static void SetX(float x, Align alignment, RectTransform rect)
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rect.transform);
        float scaleX = rect.localScale.x;
        float width = rect.sizeDelta.x * scaleX; // bounds.size.x;//
        float pivotX = width * rect.pivot.x; // pivot in local coord

        // remove pivot
        x += pivotX;

        if (alignment == Align.origin)
        {
            x -= pivotX;
        }
        else if (alignment == Align.right || alignment == Align.topRight || alignment == Align.bottomRight)
        {
            x -= width;
        }
        else if (alignment == Align.center || alignment == Align.top || alignment == Align.bottom)
        {
            x -= width / 2f;
        }
        else // left, bottomLeft, topLeft
        {
            // x = x
        }

        rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
        //bounds.min = new Vector2(x, bounds.min.y);
    }

    public float GetX()
    {
        return GetX(Align.origin);
    }

    public float GetX(Align alignment)
    {
        return GetX(alignment, GetRectTransform());
    }

    public static  float GetX(Align alignment, RectTransform rect)
    {

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rect.transform);
        float scaleX = rect.localScale.x;
        float width = rect.sizeDelta.x * scaleX;// bounds.size.x;//
        float pivotX = width * rect.pivot.x; // pivot in local coord
        float minX = rect.anchoredPosition.x - pivotX; //bounds.min.x;//  


        // remove pivot
        // minX -= pivotX;

        if (alignment == Align.origin)
        {
            return minX + pivotX;
        }
        else if (alignment == Align.right || alignment == Align.topRight || alignment == Align.bottomRight)
        {
            return minX + width;
        }
        else if (alignment == Align.center || alignment == Align.top || alignment == Align.bottom)
        {
            return minX + width / 2f;
        }
        else // left, bottomLeft, topLeft
        {
            return minX;
        }
    }


    // y

    public float GetY()
    {
        return GetY(Align.origin);
    }

    public float GetY(Align alignment)
    {
        return GetY(alignment, GetRectTransform());
    }

    public static float GetY(Align alignment, RectTransform rect)
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rect.transform);
        float scaleY = rect.localScale.y;
        float height = rect.sizeDelta.y * scaleY; // bounds.size.y;//
        float pivotY = height * rect.pivot.y; // pivot in local coord
        float minY = rect.anchoredPosition.y - pivotY; // bounds.min.y;//
        //Vector3[] corners = new Vector2[4]
        //rect.GetWorldCorners(corners = new Vector3[4]);

        // remove pivot
        //y -= pivotY;

        if (alignment == Align.origin)
        {
            return minY + pivotY;
        }
        else if (alignment == Align.top || alignment == Align.topLeft || alignment == Align.topRight)
        {
            return minY + height;
        }
        else if (alignment == Align.center || alignment == Align.left || alignment == Align.right)
        {
            return minY + height / 2f;
        }
        else // bottom, bottomLeft, bottomRight
        {
            return minY;
        }
    }



    public void SetY(float y)
    {
        SetY(y, Align.origin);
    }

    public void SetY(float y, Align alignment)
    {
        SetY(y, alignment, GetRectTransform());
    }

    public static void SetY(float y, Align alignment, RectTransform rect)
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rect.transform);
        float scaleY = rect.localScale.y;
        float height = rect.sizeDelta.y * scaleY; // bounds.size.y;//
        float pivotY = height * rect.pivot.y; // pivot in local coord



        // remove pivot
        y += pivotY;

        if (alignment == Align.origin)
        {
            y -= pivotY;
        }
        else if (alignment == Align.top || alignment == Align.topLeft || alignment == Align.topRight)
        {
            y -= height;
        }
        else if (alignment == Align.center || alignment == Align.right || alignment == Align.left)
        {
            y -= height / 2f;
        }
        else // bottom, bottomLeft, bottomRight
        {
            //y = y
        }

        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        //bounds.min = new Vector2(bounds.min.x, y);
    }


    



    // -- size --

    /// <summary>
    /// sets the RectTransform.sizeDelta
    /// </summary>
    /// <param name="size"></param>
    public void SetSize(Vector2 size)
    {
        GetRectTransform().sizeDelta = size;
    }

    /// <summary>
    /// sets the RectTransform.sizeDelta
    /// </summary>
    public void SetSize(float width, float height)
    {
        SetSize(new Vector2(width, height));
    }

    /// <summary>
    /// returns the RectTransform.sizeDelta
    /// </summary>
    public Vector2 GetSize()
    {
        return GetRectTransform().sizeDelta;
    }

    /// <summary>
    /// returns RectTransform.sizeDelta.x
    /// </summary>
    public float GetWidth()
    {
        return GetSize().x;
    }

    /// <summary>
    /// sets RectTransform.sizeDelta.x
    /// </summary>
    public void SetWidth(float width)
    {
        SetSize(width, GetHeight());
    }


    /// <summary>
    /// returns the RectTransform.sizeDelta.y
    /// </summary>
    public float GetHeight()
    {
        return GetSize().y;
    }

    /// <summary>
    /// sets RectTransform.sizeDelta.y
    /// </summary>
    public void SetHeight(float height)
    {
        SetSize(GetWidth(), height);
    }


    // -- bounds --

    public Bounds GetBounds()
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(GetRectTransform());
        return bounds;
    }



    // -- utils --

    public RectTransform GetRectTransform()
    {
        return GetRectTransform(this.gameObject);
    }

    public RectTransform GetRectTransform(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        return rt;
    }

    public Rect GetRect()
    {
        return GetRect(this.gameObject);
    }

    public Rect GetRect(GameObject obj)
    {
        return GetRectTransform(obj).rect;
    }

    // -- containts- 

    public bool ContainsScreenPoint(Vector2 screenPoint)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(GetRectTransform(), screenPoint);
    }

    // -- overlap --

    /// <summary>
    /// true ift the THIS actor overlaps the other actor on THIS actor's alignment
    /// <br>
    /// Example: if it overlaps at Align.right, it cant overlap at the  bottom, top or left. The overlap can
    /// occur ont he right upper or lower edge though, except This actors is bigger then the other
    /// actor's size
    /// </br> 
    /// </summary>
    /*public bool OverlapsAt(Align alignment, Actor other)
    {
        bool overlaps = Overlaps(other);
        if (!overlaps) return false;

        bool checkX = false, checkY = false;

        switch (alignment)
        {
            case Align.right:
                checkX = GetX() < other.GetX();
                checkY = true;    
                break;
            case Align.left:
                checkX = GetX(Align.right) > other.GetX(Align.right);
                checkY = true;
                break;
            case Align.top:
                checkY = GetY() < other.GetY();
                checkX = true;
                break;
            case Align.bottom:
                checkY = GetY(Align.top) > other.GetY(Align.top);
                checkX = true;
                break;
        }

        return checkX && checkY;
    }*/




    // -- input --



    /// <summary>
    /// return true if the mouse is over the actor
    /// </summary>
    public bool IsMouseOver()
    {
        return ContainsScreenPoint(Input.mousePosition);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        
    }



    // -- static --

    public static Actor Get(GameObject obj)
    {
        return Get<Actor>(obj);
    }

    /*
    // causes to much issues with Start() and GetStartPos
    public static Actor GetOrAdd(GameObject obj)
    {
        return GetOrAdd<Actor>(obj);
    }*/

    public static T Get<T>(GameObject obj) where T : Actor
    {
        T component = obj.GetComponent<T>();
        return component;
    }

    /*
    public static T GetOrAdd<T>(GameObject obj) where T : Actor
    {
        T a = Get<T>(obj);
        if (a == null)
        {
            obj.AddComponent<T>();
            a = obj.GetComponent<T>();
        }
        return a;
    }*/

}
