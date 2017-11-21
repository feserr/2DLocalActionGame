using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Image = UnityEngine.UI.Image;

namespace UnityStandardAssets.CrossPlatformInput
{
	public class JoystickRight : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
	{
		public enum AxisOption
		{
			// Options for which axes to use
			Both, // Use both
			OnlyHorizontal, // Only horizontal
			OnlyVertical // Only vertical
		}

        public enum JoystickSide
        {
            // Options for which joystick side
            Left,
            Right,
        }

		public int MovementRange = 100;
		public AxisOption axesToUse = AxisOption.Both; // The options for the axes that the still will use
        public JoystickSide joystickSide = JoystickSide.Left; // The options for the joystick side
		public string horizontalAxisName = "RightJoystickHorizontal"; // The name given to the horizontal axis for the cross platform input
		public string verticalAxisName = "RightJoystickVertical"; // The name given to the vertical axis for the cross platform input

        private Image m_image;
		private Vector3 m_StartPos;
		private bool m_UseX; // Toggle for using the x axis
		private bool m_UseY; // Toggle for using the Y axis
		private CrossPlatformInputManager.VirtualAxis m_HorizontalVirtualAxis; // Reference to the joystick in the cross platform input
		private CrossPlatformInputManager.VirtualAxis m_VerticalVirtualAxis; // Reference to the joystick in the cross platform input
        public int m_currentTouchIDRight;
        private JoystickLeft m_joystickLeft;

        public int CurrentTouchID
        { get { return m_currentTouchIDRight; } }

		void OnEnable()
		{
			CreateVirtualAxes();
		}

        void Start()
        {
            // Start with the joystick deactivated
            m_image = this.GetComponent<Image>();
            m_image.enabled = false;

            m_StartPos = transform.position;

            m_joystickLeft = GameObject.Find("LeftMobileJoystick").GetComponent<JoystickLeft>();

            m_currentTouchIDRight = -1;
        }

        void Update()
        {
            for (var i = 0; i < Input.touchCount; ++i)
            {
                if(i != -1)
                {
                    if((m_currentTouchIDRight == -1 || m_currentTouchIDRight > i) &&
                        (m_joystickLeft.CurrentTouchID == -1 || m_joystickLeft.CurrentTouchID != i))
                    {
                        Touch t_touch = Input.GetTouch(i);

                        if(t_touch.position.x > (Screen.width / 2))
                        {
                            m_currentTouchIDRight = i;
                            break;
                        }
                    }
                }
            }

            if(m_currentTouchIDRight != -1)
            {
                Touch touch = Input.GetTouch(m_currentTouchIDRight);

                if(touch.phase == TouchPhase.Began)
                {
                    if(touch.position.x > (Screen.width / 2))
                    {
                        transform.position = touch.position;
                        m_StartPos = transform.position;
                        m_image.enabled = true;
                    }
                }
                else if(touch.phase == TouchPhase.Moved)
                {
                    Vector3 newPos = Vector3.zero;

                    if(m_UseX)
                    {
                        int delta = (int)(touch.position.x - m_StartPos.x);
                        delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
                        newPos.x = delta;
                    }

                    if(m_UseY)
                    {
                        int delta = (int)(touch.position.y - m_StartPos.y);
                        delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
                        newPos.y = delta;
                    }
                    transform.position = new Vector3(m_StartPos.x + newPos.x, m_StartPos.y + newPos.y, m_StartPos.z + newPos.z);
                    UpdateVirtualAxes(transform.position);
                }
                else if(touch.phase == TouchPhase.Ended)
                {
                    transform.position = m_StartPos;
                    UpdateVirtualAxes(m_StartPos);
                    m_image.enabled = false;

                    m_currentTouchIDRight = -1;
                }
            }
        }

		void UpdateVirtualAxes(Vector3 value)
		{
			var delta = m_StartPos - value;
			delta.y = -delta.y;
			delta /= MovementRange;
			if (m_UseX)
			{
				m_HorizontalVirtualAxis.Update(-delta.x);
			}

			if (m_UseY)
			{
				m_VerticalVirtualAxis.Update(delta.y);
			}
		}

		void CreateVirtualAxes()
		{
			// set axes to use
			m_UseX = (axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyHorizontal);
			m_UseY = (axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyVertical);

			// create new axes based on axes to use
			if (m_UseX)
			{
				m_HorizontalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(horizontalAxisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_HorizontalVirtualAxis);
			}
			if (m_UseY)
			{
				m_VerticalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(verticalAxisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_VerticalVirtualAxis);
			}
		}


		public void OnDrag(PointerEventData data)
		{
            //Vector3 newPos = Vector3.zero;

            //if (m_UseX)
            //{
            //    int delta = (int)(data.position.x - m_StartPos.x);
            //    delta = Mathf.Clamp(delta, - MovementRange, MovementRange);
            //    newPos.x = delta;
            //}

            //if (m_UseY)
            //{
            //    int delta = (int)(data.position.y - m_StartPos.y);
            //    delta = Mathf.Clamp(delta, -MovementRange, MovementRange);
            //    newPos.y = delta;
            //}
            //transform.position = new Vector3(m_StartPos.x + newPos.x, m_StartPos.y + newPos.y, m_StartPos.z + newPos.z);
            //UpdateVirtualAxes(transform.position);
		}


		public void OnPointerUp(PointerEventData data)
		{
			transform.position = m_StartPos;
			UpdateVirtualAxes(m_StartPos);
		}


		public void OnPointerDown(PointerEventData data)
        {
            m_image.enabled = true;
        }

        public void SetImage(bool enable)
        {
            m_image.enabled = enable;
        }

		void OnDisable()
		{
			// remove the joysticks from the cross platform input
			if (m_UseX)
			{
				m_HorizontalVirtualAxis.Remove();
			}
			if (m_UseY)
			{
				m_VerticalVirtualAxis.Remove();
			}
		}
	}
}