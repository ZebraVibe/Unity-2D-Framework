using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// actable " functional interface "

/// <summary>
/// returning true stops the action before isTimeUp(). returning false stops
/// the action when isTimeUp()
/// 
/// Author: Sebastian Krahnke
/// </summary>
public delegate bool Actable(Action action);


/// <summary>
/// When using Actable: 
/// returning true stops the action before isTimeUp(). returning false stops
/// the action when isTimeUp()
/// 
/// Author: Sebastian Krahnke
/// </summary>
public class Action
{
    public const int FOREVER = -1;

    public float maxTime;
    /// <summary>
    /// ascending
    /// </summary>
    public float time;
    /// <summary>
    /// ascending. if the action maxTime is 0 the percent is set to 1
    /// </summary>
    public float percent;
    public Actor actor;
    public bool pause;


    private Actable actable;
    private bool start = true;
    private bool cancel;


    /// <summary>
    /// action which ends after the given time is up or actable returns true
    /// </summary>
    public Action(float seconds, Actable actable)
    {
        this.maxTime = seconds;
        this.actable = actable == null ? a => { return true; } : actable;
    }

    /// <summary>
    /// action which is called once
    /// </summary>
    /// <param name="actable"></param>
    public Action(Actable actable) : this(0, actable) { }

    /// <summary>
    /// acts out the action. returns true if the action is over
    /// </summary>
    public virtual bool Act<T>(float delta, T actor) where T : Actor
    {
        percent = maxTime != 0 ? time / maxTime : 1;
        this.actor = actor;

        if (IsCanceled()) return true;
        if (IsPause()) return false;

        if (actable(this) || IsTimeUp()) return true;

        if (IsStart()) start = false;
        if (!IsForever()) time += Time.deltaTime;
        if (time > maxTime) time = maxTime;
        return false;
    }

    /// <summary>
    /// Calling this inside an actable may cause errors
    /// </summary>
    /// <param name="a"></param>
    public void SetActable(Actable a)
    {
        this.actable = a;
    }

    /// <summary>
    /// Weather this action loops ofrever or not
    /// </summary>
    /// <returns></returns>
    public bool IsForever()
    {
        return maxTime < 0;
    }

    public bool IsTimeUp()
    {
        return !IsForever() && time >= maxTime;
    }

    public bool IsStart()
    {
        return start;
    }

    public bool IsPause()
    {
        return pause;
    }

    public bool IsCanceled()
    {
        return cancel;
    }


    /// <summary>
    /// Cancels/Ends the action in its current state without further acting 
    /// and removes it by returning true
    /// </summary>

    public void Cancel()
    {
        cancel = true;
    }

    /// <summary>
    /// if smooth is true : when the timer is running the time is not set back to
    /// 0 but starts at Time.deltaTime to avoid percent being 0.
    /// Also sets pause to false.
    /// </summary>
    public virtual void Restart(bool smooth)
    {
        time = !start && smooth ? Time.deltaTime : 0;
        start = true;
        pause = false;
    }





    // ---------------- static methods ----------------
    // note: maybe move them into an Actions class in the future





    // -- run --

    public static Action Run(System.Action runnable)
    {
        return Run(0, runnable);
    }

    public static Action Run(float time, System.Action runnable)
    {
        return Run(time, a => { runnable?.Invoke(); return false; });
    }

    /// <summary>
    /// creates a new action which runs once
    /// </summary>
    /// <param name="actable"></param>
    /// <returns></returns>
    public static Action Run(Actable actable)
    {
        return Run(0, actable);
    }

    public static Action Run(float time, Actable actable)
    {
        return new Action(time, actable);
    }



    // -- alpha--

    /// <summary>
    /// Add a CanvasGroup Component to the parent to change children alpha as well
    /// </summary>
    public class AlphaAction : Action
    {
        float endAlpha;
        public AlphaAction(float time, float alpha, Interpolation interpolation) : base(time, null)
        {
            Image img = null;
            TextMeshProUGUI tmpGUI = null;
            float startAlpha = 1;
            this.endAlpha = alpha;
            this.actable =  a =>
            {
                if (a.IsStart())
                {
                    img = a.actor.GetComponent<Image>();
                    if (img != null)
                    {
                        startAlpha = img.color.a;
                    }
                    tmpGUI = a.actor.GetComponent<TextMeshProUGUI>();
                    if (tmpGUI != null)
                    {
                        if (tmpGUI != null) startAlpha = tmpGUI.color.a;
                    }
                }

                float inter = interpolation == null ? a.percent : interpolation(a.percent);
                float parentAlpha = startAlpha + (endAlpha - startAlpha) * inter;
                Color c ;

                //parent 
                if (img != null) // image
                {
                    c = img.color;
                    img.color = new Color(c.r, c.g, c.b, parentAlpha);
                }
                if (tmpGUI != null)//tmpro
                {
                    c = tmpGUI.color;
                    tmpGUI.color = new Color(c.r, c.g, c.b, parentAlpha);
                }

                // children 
                CanvasGroup cg = a.actor.GetComponent<CanvasGroup>();
                if(cg != null)
                {
                    cg.alpha = parentAlpha;
                }


                return false;
            };
        }

        public override void Restart(bool smooth)
        {
            base.Restart(smooth);
        }

        public virtual void Restart(float alpha, bool smooth)
        {
            base.Restart(smooth);
            endAlpha = alpha;

        }

    }

    public static AlphaAction Alpha(float alpha)
    {
        return Alpha(0, alpha);
    }

    public static AlphaAction Alpha(float time, float alpha)
    {
        return Alpha(time, alpha, null);
    }

    public static AlphaAction Alpha(float time, float alpha, Interpolation interpolation)
    {
        return new AlphaAction(time, alpha, interpolation);
    }



    // -- fade --

    public static AlphaAction FadeIn(float time)
    {
        return FadeIn(time, null);
    }

    public static AlphaAction FadeIn(float time, Interpolation interpolation)
    {
        //return Parallel(Alpha(0), Alpha(time, 1, interpolation));
        return Alpha(time, 1, interpolation);
    }

    public static AlphaAction FadeOut(float time)
    {
        return FadeOut(time, null);
    }

    public static AlphaAction FadeOut(float time, Interpolation interpolation)
    {
        //return Parallel(Alpha(1), Alpha(time, 0, interpolation));
        return Alpha(time, 0, interpolation);
    }



    // - -sequence --

    public class SequenceAction : Action
    {
        private List<Action> list = new List<Action>();

        public SequenceAction(params Action[] actions) : base(FOREVER, null)
        {
            this.list.AddRange(actions);
            IEnumerator e = null;
            this.actable = a =>
            {
                if (IsStart())
                {
                    // sets Current to first "0" index
                    e = list.GetEnumerator();
                    if (!e.MoveNext()) return true;
                }
                if (((Action)e.Current).Act(Time.deltaTime, a.actor)) {
                    if (!e.MoveNext())
                    {
                        return true;
                    }
                }
                return false;
            };
        }



        public override void Restart(bool smooth)
        {
            list.ForEach(a => a.Restart(smooth));
            base.Restart(smooth);
        }


    }

    public static SequenceAction Sequence(params Action[] actions)
    {
        return new SequenceAction(actions);
    }

    public static Action Target(Actor target, Action action)
    {
        return new Action(action.maxTime, a =>
        {
            if (a.IsStart())
            {
                action.Restart(true);
            }

            return action.Act(Time.deltaTime, target);
        });
    }



    // -- delay & wait --

    public static Action Delay(float time, Action action)
    {
        return Sequence(Wait(time), action);
    }

    public static Action Wait(float time)
    {
        return Run(time, a => { return false; });
    }



    // -- repeat --

    public class RepeatAction : Action
    {
        private Action action;
        int currentCount;

        public RepeatAction(int count, Action action) : base(Action.FOREVER, null)
        {
            currentCount = count;
            this.action = action;
            this.actable = a => {
                if (action.Act(Time.deltaTime, a.actor))
                {
                    if (currentCount == FOREVER) action.Restart(true);
                    else
                    {
                        if (currentCount > 0)
                        {
                            action.Restart(true);
                            currentCount--;
                        }
                        else return true;
                    }
                }
                return false;
            };
        }


        public override void Restart(bool smooth)
        {
            action.Restart(smooth);
            base.Restart(smooth);
        }
    }

    public static RepeatAction Repeat(int count, Action action)
    {
        return new RepeatAction(count, action);
    }



    // -- parallel --

    public class ParallelAction : Action
    {
        private List<Action> actionList = new List<Action>();
        private List<Action> list = new List<Action>();
        private List<Action> copy = new List<Action>();

        public ParallelAction(params Action[] actions) : base(FOREVER, null)
        {
            if (actions != null) actionList.AddRange(actions);
            this.actable = a =>
            {
                if (a.IsStart())
                {
                    list.AddRange(actionList);
                }

                copy.AddRange(list);

                copy.ForEach(b =>
                {

                    if (b.Act(Time.deltaTime, a.actor)) list.Remove(b);

                });
                copy.Clear();
                if (list.Count == 0) return true;
                return false;
            };
        }

        public override void Restart(bool smooth)
        {
            actionList.ForEach(a => a.Restart(smooth));
            list.Clear();
            copy.Clear();
            base.Restart(smooth);
        }
    }

    public static Action Parallel(params Action[] actions)
    {
        return new ParallelAction(actions);
    }



    // -- after --

    /// <summary>
    /// Appends the list with the action after the currently last added action is done
    /// with the new action. 
    /// </summary>
    public static Action After(Action action)
    {
        List<Action> actions = new List<Action>();
        return new Action(a =>
        {
            actions.Clear();
            actions.AddRange(a.actor.GetActions());
            actions.Remove(a);

            a.actor.RemoveAllActions();
            a.actor.AddAction(
                Sequence(
                    Parallel(actions.ToArray()), 
                    action));

            return false;
        });
    }



    // -- size to --


    /// <summary>
    /// sizes immediately
    /// </summary>
    /// <param name="toSize"></param>
    /// <returns></returns>
    public static Action SizeTo(Vector2 toSize)
    {
        return SizeTo(0, toSize);
    }

    public static Action SizeTo(float time, float width, float height)
    {
        return SizeTo(time, new Vector2(width, height));
    }

    public static Action SizeTo(float time, Vector2 toSize)
    {
        return SizeTo(time, toSize, null);
    }

    public static Action SizeTo(float time, Vector2 toSize, Interpolation interpolation)
    {

        Vector2 fromSize = new Vector2();
        return new Action(time, a => 
        {
            if (a.IsStart())
            {
                fromSize = a.actor.GetSize();
            }
            Vector2 diff = toSize - fromSize;
            float inter = interpolation == null ? a.percent : interpolation(a.percent);
            a.actor.SetSize(fromSize + diff * inter);
            return false;
        });
    }

    
    // -- move by --

    public class MoveByAction : Action
    {
        Vector2 value = new Vector2(0,0);
        MoveToAction moveTo = null;

        public MoveByAction(float time, Vector2 value, Align alignment, Interpolation interpolation) : base(time, null)
        {
            //this.value.Set(value.x, value.y);
            this.value = value;
            moveTo = new MoveToAction(time, new Vector2(), alignment, interpolation);
            actable = a =>
            {
                if (a.IsStart())
                {
                    Vector2 toPos = a.actor.GetPosition(alignment) + this.value;
                    moveTo.Restart(toPos, true);
                }

                return moveTo.Act(Time.deltaTime, a.actor);
            };
        }

        public virtual void Restart(Vector2 newValue, bool smooth)
        {
            Restart(smooth);
            this.value = newValue;
        }
    }

    /// <summary>
    /// immediate movement
    /// </summary>
    public static MoveByAction MoveBy(float x, float y)
    {
        return MoveBy(0, x, y);
    }

    /// <summary>
    /// immediate movement
    /// </summary>
    public static MoveByAction MoveBy(Vector2 value)
    {
        return MoveBy(0, value);
    }

    public static MoveByAction MoveBy(float time, float x, float y)
    {
        return MoveBy(time, x , y, null);
    }

    public static MoveByAction MoveBy(float time, float x, float y, Interpolation interpolation)
    {
        return MoveBy(time, new Vector2(x, y), interpolation);
    }

    /// <summary>
    /// To do a looped movement call : Action.Repeat(Action.FOREVER, ActionMoveBy())
    /// </summary>
    public static MoveByAction MoveBy(float time, Vector2 value)
    {
        return MoveBy(time, value, null);
    }

    /// <summary>
    /// To do a looped movement call : Action.Repeat(Action.FOREVER, ActionMoveBy())
    /// </summary>
    public static MoveByAction MoveBy(float time, Vector2 value, Interpolation interpolation)
    {
        return new MoveByAction(time, value, Align.origin, interpolation);
    }



    // -- move by aligned --

    public static MoveByAction MoveByAligned(float time, Vector2 value, Align alignment)
    {
        return MoveByAligned(time, value, alignment, null);
    }

    public static MoveByAction MoveByAligned(float time, Vector2 value, Align alignment, Interpolation interpolation)
    {
        return new MoveByAction(time, value, alignment, interpolation);
    }



    // -- move to --

    public class MoveToAction : Action
    {
        Vector2 toPos = new Vector2();
        Vector2 fromPos = new Vector2();

        public MoveToAction(float time, Vector2 toPos, Align alignment, Interpolation interpolation) : base(time, null)
        {
            this.toPos = toPos;
            actable = a =>
            {
                if (a.IsStart())
                {
                    fromPos = a.actor.GetPosition(alignment);
                }
                Vector2 diff = this.toPos - fromPos;
                float inter = interpolation == null ? a.percent : interpolation(a.percent);
                a.actor.SetPosition(fromPos + diff * inter, alignment);
                return false;
            };
        }

        public virtual void Restart(Vector2 newToPos, bool smooth)
        {
            Restart(smooth);
            this.toPos = newToPos;
        }



    }

    /// <summary>
    /// immediate movement
    /// </summary>
    public static MoveToAction MoveTo(float x, float y)
    {
        return MoveTo(0, x, y);
    }

    /// <summary>
    /// immediate movement
    /// </summary>
    public static MoveToAction MoveTo(Vector2 toPos)
    {
        return MoveTo(0, toPos);
    }

    /// <summary>
    /// immediate movement
    /// </summary>
    public static MoveToAction MoveTo(float time, float x, float y)
    {
        return MoveTo(time, new Vector2(x, y));
    }
    
    /// <summary>
    /// use anchored pos
    /// </summary>
    public static MoveToAction MoveTo(float time, Vector2 toPos)
    {
        return MoveTo(time, toPos, null);
    }

    /// <summary>
    /// use anchored pos
    /// </summary>
    public static MoveToAction MoveTo(float time, Vector2 toPos, Interpolation interpolation)
    {
        return new MoveToAction(time, toPos, Align.origin, interpolation);
    }



    // -- move to  aligned --

    public static MoveToAction MoveToAligned(Vector2 toPos, Align alignment)
    {
        return MoveToAligned(0, toPos, alignment, null);
    }

    public static MoveToAction MoveToAligned(float time, Vector2 toPos, Align alignment)
    {
        return MoveToAligned(time, toPos, alignment, null);
    }

    public static MoveToAction MoveToAligned(float time, Vector2 toPos, Align alignment, Interpolation interpolation)
    {
        return new MoveToAction(time, toPos, alignment, interpolation);
    }




    // -- scale --

    public class ScaleAction : Action
    {
        private Align alignment = Align.origin;
        private Vector2 localOrigin = Vector2.zero;
        private Interpolation interpolation;
        private bool hasAlignment;

        public ScaleAction(float time, Vector2 scale, Align alignment, Interpolation interpolation) : base(time, null)
        {
            this.alignment = alignment;
            this.interpolation = interpolation;
            hasAlignment = true;
            actable = createActable(scale);
        }

        public ScaleAction(float time, Vector2 scale, Vector2 localOrigin, Interpolation interpolation) : base(time, null)
        {
            this.localOrigin = localOrigin;
            this.interpolation = interpolation;
            actable = createActable(scale);
        }

        private Actable createActable(Vector2 scale)
        {
            Vector2 diff = Vector2.zero;
            Vector2 startScale = Vector2.zero;
            return a =>
            {
                if (a.IsStart())
                {
                    startScale = a.actor.GetScale();
                    diff = scale - startScale;
                }

                float inter = interpolation == null ? a.percent : interpolation(a.percent);

                if (hasAlignment) a.actor.ScaleAround(startScale + diff * inter, alignment);
                else a.actor.ScaleAround(startScale + diff * inter, localOrigin);
                return false;
            };
        }
    }

    public static ScaleAction ScaleTo(Vector2 scale, Align alignment)
    {
        return ScaleTo(0, scale, alignment);
    }

    public static ScaleAction ScaleTo(float time, Vector2 scale, Align alignment)
    {
        return ScaleTo(time, scale, alignment, null);
    }

    public static ScaleAction ScaleTo(float time, Vector2 scale, Align alignment, Interpolation interpolation)
    {
        return new ScaleAction(time, scale, alignment, interpolation);
    }

    public static ScaleAction ScaleTo(Vector2 scale, Vector2 localOrigin)
    {
        return ScaleTo(0, scale, localOrigin);
    }

    public static ScaleAction ScaleTo(float time, Vector2 scale, Vector2 localOrigin)
    {
        return ScaleTo(time, scale, localOrigin, null);
    }

    public static ScaleAction ScaleTo(float time, Vector2 scale, Vector2 localOrigin, Interpolation interpolation)
    {
        return new ScaleAction(time, scale, localOrigin, interpolation);
    }


    // -- active --

    /// <summary>
    /// toggles the gameobjects active state of the actor using this action
    /// </summary>
    public Action ToggleActive()
    {
        return ToggleActive(null);
    }

    /// <summary>
    /// toggles the gameobjects active state of the given target
    /// </summary>
    public Action ToggleActive(Actor target)
    {
        return Run(a =>
        {
            if(target == null)a.actor.gameObject.SetActive(!target.gameObject.activeSelf);
            else target.gameObject.SetActive(!target.gameObject.activeSelf);
            return false;
        });
    }


    // -- touchable --

    /// <summary>
    /// changes the touchable attribute of the actor using this action
    /// </summary>
    public static Action Touchable(Touchable touchable)
    {
        return Touchable(null, touchable);
    }

    public static Action Touchable(Actor target, Touchable touchable)
    {
        return Run(a =>
        {
            if (target == null) a.actor.touchable = touchable;
            else target.touchable = touchable;
            return false;
        });
    }


    // -- convinience --

    /// <summary>
    /// An Infinitly looping action that handles states.
    /// Note that only the default state action is allowed to be null.
    /// </summary>
    public class StateHandlerAction<T> : Action where T : System.Enum
    {
        T defaultState;
        T currentState;
        Dictionary<T, Action> states;
        Action currentStateAction;

        /// <summary>
        /// An Infinitly looping action that handles states.
        /// Note that only the default state action is allowed to be null.
        /// </summary>
        public StateHandlerAction(T defaultState, Dictionary<T, Action> states) : base(FOREVER, null)
        {
            this.states = states;
            this.defaultState = defaultState;

            SetActable(a =>
            {
                // default state that is nullable
                if (currentState.Equals(defaultState))
                {
                    if (currentStateAction == null) return false;
                    else if (currentStateAction.Act(Time.deltaTime, a.actor))
                    {
                        currentStateAction.Restart(true);
                    }
                } // none default state
                else
                {
                    if (currentStateAction.Act(Time.deltaTime, a.actor))
                    {
                        SetState(defaultState);
                    }
                }
                return false;
            });

            SetState(defaultState);
        }



        public void SetState(T state)
        {
            if (currentStateAction != null)
            {
                // reset
                currentStateAction.pause = true;
                currentStateAction.Restart(true);
            }
            currentState = state;
            states.TryGetValue(currentState, out currentStateAction);
            if (currentStateAction != null)
            {
                currentStateAction.pause = false;
                currentStateAction.Restart(true); // in case the action has been changed
            }
        }

        public override void Restart(bool smooth)
        {
            foreach(var state in states)state.Value.Restart(smooth);
            base.Restart(smooth);
        }

    }

    public static StateHandlerAction<T> StateHandler<T>(T state, Dictionary<T, Action> states) where T : System.Enum
    {
        return new StateHandlerAction<T>(state, states);
    }


    // -- util --

    public static Action DebugText(string text)
    {
        return new Action(a =>
        {
            Debug.Log(text);
            return true;
        });
    }
}
