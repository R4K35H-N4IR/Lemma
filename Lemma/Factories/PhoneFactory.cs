﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using Microsoft.Xna.Framework.Input;

namespace Lemma.Factories
{
	public class PhoneFactory : Factory
	{
		public PhoneFactory()
		{
			this.Color = new Vector3(0.4f, 0.4f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "Phone");
			result.Add("Phone", new Phone());

			result.Add("Active", new Property<bool> { Value = false });
			result.Add("Attached", new Property<bool> { Value = false });

			PhysicsBlock physics = new PhysicsBlock();
			physics.Size.Value = new Vector3(0.4f, 0.25f, 0.25f);
			physics.Mass.Value = 0.025f;
			result.Add("Physics", physics);

			Model model = new Model();
			result.Add("Model", model);
			model.Filename.Value = "Models\\phone";
			model.Color.Value = new Vector3(0.2f);
			model.Editable = false;

			PointLight light = new PointLight();
			light.Color.Value = Vector3.One;
			light.Attenuation.Value = 4.0f;
			light.Shadowed.Value = false;
			result.Add("Light", light);

			PlayerTrigger trigger = new PlayerTrigger();
			trigger.Radius.Value = 3.0f;
			result.Add("Trigger", trigger);

			Transform transform = new Transform();
			result.Add("Transform", transform);

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			Transform transform = result.Get<Transform>();
			Model model = result.Get<Model>();
			PlayerTrigger trigger = result.Get<PlayerTrigger>();
			PhysicsBlock physics = result.Get<PhysicsBlock>();
			Phone phone = result.Get<Phone>();
			PCInput input = new PCInput();
			result.Add("Input", input);

			PointLight light = result.Get<PointLight>();

			UIRenderer ui = new UIRenderer();
			result.Add("UIRenderer", ui);

			Sprite phoneSprite = new Sprite();
			phoneSprite.Image.Value = "Images\\phone";
			phoneSprite.AnchorPoint.Value = new Vector2(0.5f, 0);
			phoneSprite.Name.Value = "phone";
			phoneSprite.Visible.Value = false;
			ui.Root.Children.Add(phoneSprite);

			Scroller phoneScroller = new Scroller();
			phoneScroller.AnchorPoint.Value = new Vector2(0.5f, 0);
			phoneScroller.Name.Value = "scroller";
			phoneSprite.Children.Add(phoneScroller);

			ListContainer messageList = new ListContainer();
			messageList.Name.Value = "messages";
			messageList.AnchorPoint.Value = new Vector2(0, 0);
			messageList.Spacing.Value = 20.0f;
			phoneScroller.Children.Add(messageList);

			Container notificationContainer = new Container();
			notificationContainer.Tint.Value = Microsoft.Xna.Framework.Color.Black;
			notificationContainer.Opacity.Value = 0.75f;
			notificationContainer.AnchorPoint.Value = new Vector2(1, 1);
			notificationContainer.Visible.Value = false;
			notificationContainer.PaddingLeft.Value = 8.0f;
			ui.Root.Children.Add(notificationContainer);

			ListContainer notificationLayout = new ListContainer();
			notificationLayout.Orientation.Value = ListContainer.ListOrientation.Horizontal;
			notificationLayout.Alignment.Value = ListContainer.ListAlignment.Middle;
			notificationContainer.Children.Add(notificationLayout);

			TextElement notificationText = new TextElement();
			notificationText.FontFile.Value = "Font";
			notificationLayout.Children.Add(notificationText);

			Sprite phoneLight = new Sprite();
			phoneLight.Image.Value = "Images\\phone-light";
			phoneLight.Name.Value = "phone-light";
			phoneLight.Opacity.Value = 0.0f;
			notificationLayout.Children.Add(phoneLight);

			Container composeButton = new Container();
			composeButton.Tint.Value = new Color(0.1f, 0.1f, 0.1f);
			composeButton.Name.Value = "compose-button";
			composeButton.AnchorPoint.Value = new Vector2(0, 0);
			phoneSprite.Children.Add(composeButton);

			TextElement composeMessage = new TextElement();
			composeMessage.FontFile.Value = "Font";
			composeMessage.Text.Value = "Compose";
			composeButton.Children.Add(composeMessage);

			Container responseMenu = new Container();
			responseMenu.Tint.Value = new Color(0.1f, 0.1f, 0.1f);
			responseMenu.Opacity.Value = 1.0f;
			responseMenu.Name.Value = "response-menu";
			responseMenu.AnchorPoint.Value = new Vector2(0, 1);
			responseMenu.Visible.Value = false;
			phoneSprite.Children.Add(responseMenu);

			ListContainer responseList = new ListContainer();
			responseList.Orientation.Value = ListContainer.ListOrientation.Vertical;
			responseList.Name.Value = "responses";
			responseMenu.Children.Add(responseList);

			Property<bool> active = result.GetProperty<bool>("Active");

			Property<bool> attached = result.GetProperty<bool>("Attached");

			phone.Add(new TwoWayBinding<bool>(attached, phone.Attached));

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			trigger.Add(new Binding<Vector3>(trigger.Position, transform.Position));

			physics.Add(new TwoWayBinding<Matrix>(transform.Matrix, physics.Transform));
			physics.Add(new Binding<bool>(physics.Enabled, x => !x, attached));
			model.Add(new Binding<bool>(model.Enabled, x => !x, attached));
			trigger.Add(new Binding<bool>(trigger.Enabled, x => !x, attached));
			input.Add(new Binding<bool>(input.Enabled, active));
			ui.Add(new Binding<bool>(ui.Enabled, attached));
			light.Add(new Binding<bool>(light.Enabled, x => !x, attached));
			light.Add(new Binding<Vector3>(light.Position, transform.Position));

			Command hidePhone = null;
			Command showPhone = null;

			Binding<Vector3> attachBinding = null;
			result.Add("Attach", new Command<Entity>
			{
				Action = delegate(Entity e)
				{
					attached.Value = true;
					attachBinding = new Binding<Vector3>(transform.Position, e.Get<Transform>().Position);
					result.Add(attachBinding);
				}
			});

			result.Add("Detach", new Command
			{
				Action = delegate()
				{
					attached.Value = false;
					result.Remove(attachBinding);
					if (active)
						hidePhone.Execute();
				}
			});

			result.Add(new CommandBinding<Entity>(trigger.PlayerEntered, delegate(Entity player)
			{
				player.GetProperty<Entity.Handle>("Phone").Value = result;
				showPhone.Execute();
			}));

			result.Add(new NotifyBinding(delegate()
			{
				result.CannotSuspend = attached;
				foreach (Component c in result.ComponentList.ToList())
				{
					if (c.Suspended)
						c.Suspended.Value = false;
				}
			}, attached));

			phoneSprite.Add(new Binding<Vector2, Point>(phoneSprite.Position, x => new Vector2(x.X * 0.5f, x.Y), main.ScreenSize));
			notificationContainer.Add(new Binding<Vector2, Point>(notificationContainer.Position, x => new Vector2(x.X - 20, x.Y - 20), main.ScreenSize));
			notificationContainer.Add(new Binding<bool>(notificationContainer.Visible, phone.HasUnreadMessages));
			notificationText.Add(new Binding<string, PCInput.PCInputBinding>(notificationText.Text, x => x.ToString(), ((GameMain)main).Settings.TogglePhone));

			const float padding = 20.0f;
			phoneScroller.Add(new Binding<Vector2>(phoneScroller.Position, x => new Vector2(x.X * 0.5f, 61 + padding), phoneSprite.Size));
			phoneScroller.Add(new Binding<Vector2>(phoneScroller.Size, x => new Vector2(x.X - 64 - (padding * 2.0f), x.Y - (61 + 145 + (padding * 3.0f))), phoneSprite.Size));

			composeButton.Add(new Binding<Vector2>(composeButton.Position, () => phoneScroller.Position + (new Vector2(-phoneScroller.AnchorPoint.Value.X, phoneScroller.InverseAnchorPoint.Value.Y) * phoneScroller.ScaledSize) + new Vector2(0, padding), phoneScroller.Position, phoneScroller.AnchorPoint, phoneScroller.InverseAnchorPoint, phoneScroller.ScaledSize));
			composeButton.Add(new Binding<Color>(composeButton.Tint, () => composeButton.Highlighted ? new Color(0.4f, 0.1f, 0.1f) : (phone.Responses.Size > 0 ? new Color(0.25f, 0.05f, 0.05f) : new Color(0.1f, 0.1f, 0.1f)), composeButton.Highlighted, phone.Responses.Size));

			responseMenu.Add(new Binding<Vector2>(responseMenu.Position, () => composeButton.Position + (composeButton.AnchorPoint.Value * composeButton.ScaledSize.Value), composeButton.Position, composeButton.AnchorPoint, composeButton.ScaledSize));

			responseList.Add(new ListBinding<UIComponent, Phone.Response>
			(
				responseList.Children,
				phone.Responses,
				delegate(Phone.Response response)
				{
					Container field = new Container();
					field.Tint.Value = new Color(1.0f, 1.0f, 1.0f);
					field.Opacity.Value = 0.3f;
					field.Add(new Binding<float, bool>(field.Opacity, x => x ? 0.4f : 0.2f, field.Highlighted));
					field.Add(new CommandBinding<Point>(field.MouseLeftDown, delegate(Point mouse)
					{
						Session.Recorder.Event(main, "PhoneSentMessage");
						phone.Respond(response);
						responseMenu.Visible.Value = false;
						phoneScroller.CheckLayout();
						phoneScroller.ScrollToBottom();
#if !DEVELOPMENT
						result.Add(new Animation(new Animation.Delay(0.25f), new Animation.Execute(hidePhone.Execute)));
#endif
					}));

					TextElement textField = new TextElement();
					textField.FontFile.Value = "Font";
					textField.Add(new Binding<float, Vector2>(textField.WrapWidth, x => x.X - 16.0f, phoneScroller.Size));
					textField.Text.Value = response.Text;
					field.Children.Add(textField);
					return new[] { field };
				}
			));

			composeButton.Add(new CommandBinding<Point>(composeButton.MouseLeftDown, delegate(Point mouse)
			{
				if (responseList.Children.Count > 0 || responseMenu.Visible)
					responseMenu.Visible.Value = !responseMenu.Visible;
			}));

			messageList.Add(new ListBinding<UIComponent, Phone.Message>
			(
				messageList.Children,
				phone.Messages,
				delegate(Phone.Message msg)
				{
					Container field = new Container();
					field.Tint.Value = msg.Incoming ? new Color(0.0f, 0.2f, 0.4f) : new Color(0.02f, 0.02f, 0.02f);

					TextElement textField = new TextElement();
					textField.FontFile.Value = "Font";
					textField.Text.Value = msg.Text;
					textField.Tint.Value = msg.Incoming ? new Color(1.0f, 1.0f, 1.0f) : new Color(0.7f, 0.7f, 0.7f);
					textField.Add(new Binding<float, Vector2>(textField.WrapWidth, x => x.X - 32.0f, phoneScroller.Size));
					field.Children.Add(textField);
					return new[] { field };
				}
			));

			messageList.Children.ItemAdded += delegate(int index, UIComponent msg)
			{
				phoneScroller.ScrollToBottom();
			};

			phoneScroller.ScrollToBottom();

			result.Add(new Binding<bool>(main.IsMouseVisible, active, () => attached));

			// Vibrate the phone and start the light blinking when we receive a message
			Animation lightBlinker = new Animation
			(
				new Animation.RepeatForever
				(
					new Animation.Sequence
					(
						new Animation.Set<float>(phoneLight.Opacity, 1.0f),
						new Animation.Delay(0.25f),
						new Animation.Set<float>(phoneLight.Opacity, 0.0f),
						new Animation.Delay(1.0f)
					)
				)
			);
			lightBlinker.Add(new Binding<bool>(lightBlinker.Enabled, phone.HasUnreadMessages));
			result.Add(lightBlinker);

			phone.Messages.ItemAdded += delegate(int index, Phone.Message msg)
			{
				if (!msg.Incoming) // Only for incoming messages
					return;

				if (attached)
					Sound.PlayCue(main, "Phone Vibrate");

				if (!active)
					phone.HasUnreadMessages.Value = true;
			};

			const float phoneTransitionTime = 0.25f;
			Animation phoneAnimation = null;

			MouseState mouseState = new MouseState();

			showPhone = new Command
			{
				Action = delegate()
				{
					if (phoneAnimation == null || !phoneAnimation.Active)
					{
						// Show the phone
						Session.Recorder.Event(main, "PhoneOpen");

						mouseState = main.MouseState;

						active.Value = true;
						phoneSprite.Visible.Value = true;

						if (phone.HasUnreadMessages)
							phoneScroller.ScrollToBottom();
						phone.HasUnreadMessages.Value = false;

						phoneLight.Opacity.Value = 0.0f;

						phoneAnimation = new Animation
						(
							new Animation.Parallel
							(
								new Animation.FloatMoveTo(main.Renderer.BlurAmount, 1.0f, phoneTransitionTime),
								new Animation.Vector2MoveTo(phoneSprite.AnchorPoint, new Vector2(0.5f, 1), phoneTransitionTime)
							)
						);
						result.Add(phoneAnimation);
					}
				}
			};

			result.Add("Show", showPhone);

			hidePhone = new Command
			{
				Action = delegate()
				{
					if (phoneAnimation == null || !phoneAnimation.Active)
					{
						// Hide the phone
						main.MouseState.Value = main.LastMouseState.Value = mouseState;

						active.Value = false;
						phoneAnimation = new Animation
						(
							new Animation.Parallel
							(
								new Animation.FloatMoveTo(main.Renderer.BlurAmount, 0.0f, phoneTransitionTime),
								new Animation.Vector2MoveTo(phoneSprite.AnchorPoint, new Vector2(0.5f, 0), phoneTransitionTime)
							),
							new Animation.Execute(delegate() { phoneSprite.Visible.Value = false; })
						);
						result.Add(phoneAnimation);
					}
				}
			};

			result.Add("Hide", hidePhone);

			input.Add(new CommandBinding(input.GetKeyDown(Keys.Tab), hidePhone));

			this.SetMain(result, main);

			PhysicsBlock.CancelPlayerCollisions(physics);
		}

		public override void AttachEditorComponents(Entity result, Main main)
		{
			// Don't attach the default editor components.
			PlayerTrigger.AttachEditorComponents(result, main, this.Color);
		}
	}
}
