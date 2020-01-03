// Copyright (c) 2017 Leacme (http://leac.me). View LICENSE.md for more information.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Hud : Node2D {

	private Button recordBt = new Button() { ToggleMode = true };
	private Button delayBt = new Button() { ToggleMode = true };
	private Button hiPitchBt = new Button() { ToggleMode = true };
	private Button loPitchBt = new Button() { ToggleMode = true };
	private Button amplifyBt = new Button() { ToggleMode = true };
	private Button distortionBt = new Button() { ToggleMode = true };
	private Button eq21Bt = new Button() { ToggleMode = true };
	private Button hiPassBt = new Button() { ToggleMode = true };
	private Button loPassBt = new Button() { ToggleMode = true };
	private Button bandBt = new Button() { ToggleMode = true };
	private Button panLeftBt = new Button() { ToggleMode = true };
	private Button panRightBt = new Button() { ToggleMode = true };
	private Button phaserBt = new Button() { ToggleMode = true };
	private Button reverbBt = new Button() { ToggleMode = true };

	private VBoxContainer btHolder = new VBoxContainer() { Alignment = BoxContainer.AlignMode.Center };
	private Dictionary<Button, AudioEffect> buttonEffects = new Dictionary<Button, AudioEffect>();

	private TextureRect vignette = new TextureRect() {
		Expand = true,
		Texture = new GradientTexture() {
			Gradient = new Gradient() { Colors = new[] { Colors.Transparent } }
		},
		Material = new ShaderMaterial() {
			Shader = new Shader() {
				Code = @"
					shader_type canvas_item;
					void fragment() {
						float iRad = 0.3;
						float oRad = 1.0;
						float opac = 0.5;
						vec2 uv = SCREEN_UV;
					    vec2 cent = uv - vec2(0.5);
					    vec4 tex = textureLod(SCREEN_TEXTURE, SCREEN_UV, 0.0);
					    vec4 col = vec4(1.0);
					    col.rgb *= 1.0 - smoothstep(iRad, oRad, length(cent));
					    col *= tex;
					    col = mix(tex, col, opac);
					    COLOR = col;
					}"
			}
		}
	};

	public override void _Ready() {
		InitVignette();

		var recordNode = new AudioStreamPlayer() { Autoplay = true, Stream = new AudioStreamMicrophone(), Bus = "Record" };
		AddChild(recordNode);

		btHolder.RectMinSize = GetViewportRect().Size;
		AddChild(btHolder);

		PrepButton(delayBt, "Delay");
		delayBt.Pressed = true;
		var delayEffect = (AudioEffectDelay)buttonEffects[delayBt];
		delayEffect.Feedback__active = delayEffect.Tap2__active = false;
		delayEffect.Tap1__delayMs = 5000;
		delayEffect.Dry = 0;

		PrepButton(amplifyBt, "Amplify");
		amplifyBt.Pressed = true;
		var amplifyEffect = (AudioEffectAmplify)buttonEffects[amplifyBt];
		amplifyEffect.VolumeDb = 18;

		PrepButton(hiPitchBt, "Hi-Pitch");
		((AudioEffectPitchShift)buttonEffects[hiPitchBt]).PitchScale = 2f;

		PrepButton(loPitchBt, "Low-Pitch");
		((AudioEffectPitchShift)buttonEffects[loPitchBt]).PitchScale = 0.5f;

		PrepButton(distortionBt, "Distortion");
		var distortionEffect = (AudioEffectDistortion)buttonEffects[distortionBt];
		distortionEffect.Drive = 1;
		distortionEffect.Mode = AudioEffectDistortion.ModeEnum.Lofi;

		PrepButton(hiPassBt, "Hi-Pass");
		PrepButton(loPassBt, "Low-Pass");

		PrepButton(bandBt, "Band-Pass");

		PrepButton(panLeftBt, "Pan Left");
		((AudioEffectPanner)buttonEffects[panLeftBt]).Pan = -1;
		PrepButton(panRightBt, "Pan Right");
		((AudioEffectPanner)buttonEffects[panRightBt]).Pan = 1;

		PrepButton(phaserBt, "Phaser");
		var phaserEffect = (AudioEffectPhaser)buttonEffects[phaserBt];
		phaserEffect.Feedback = 0.7f;
		phaserEffect.Depth = 1;

		PrepButton(reverbBt, "Reverb");
		var reverbEffect = (AudioEffectReverb)buttonEffects[reverbBt];
		reverbEffect.Dry = 0;

	}

	private void PrepButton(Button button, string title) {
		button.Text = title;
		StyleButton(button);
		btHolder.AddChild(button);
		button.Connect("toggled", this, nameof(OnButtonToggled), new Godot.Collections.Array { button });

		var be = InitButtonToggle(button);
		buttonEffects[button] = AudioServer.GetBusEffect(be.busIndex, be.effectIndex);
	}

	private (int busIndex, int effectIndex) InitButtonToggle(Button button) {
		var busIndex = AudioServer.GetBusIndex("Record");
		var effectIndex = buttonEffects.Keys.ToList().IndexOf(button);
		if (button.Pressed) {
			AudioServer.SetBusEffectEnabled(busIndex, effectIndex, true);
		} else {
			AudioServer.SetBusEffectEnabled(busIndex, effectIndex, false);
		}
		return (busIndex, effectIndex);
	}

	private void OnButtonToggled(bool pressed, Button button) {
		InitButtonToggle(button);
	}

	public override void _EnterTree() {
		var recordIndex = AudioServer.GetBusIndex("Master") + 1;
		AudioServer.AddBus(recordIndex);
		AudioServer.SetBusName(recordIndex, "Record");

		buttonEffects[recordBt] = new AudioEffectRecord();
		buttonEffects[hiPitchBt] = new AudioEffectPitchShift();
		buttonEffects[loPitchBt] = new AudioEffectPitchShift();
		buttonEffects[amplifyBt] = new AudioEffectAmplify();
		buttonEffects[distortionBt] = new AudioEffectDistortion();
		buttonEffects[hiPassBt] = new AudioEffectHighPassFilter();
		buttonEffects[loPassBt] = new AudioEffectLowPassFilter();
		buttonEffects[bandBt] = new AudioEffectBandPassFilter();
		buttonEffects[panLeftBt] = new AudioEffectPanner();
		buttonEffects[panRightBt] = new AudioEffectPanner();
		buttonEffects[phaserBt] = new AudioEffectPhaser();
		buttonEffects[reverbBt] = new AudioEffectReverb();
		// last
		buttonEffects[delayBt] = new AudioEffectDelay();

		buttonEffects.ToList().ForEach(z => AudioServer.AddBusEffect(recordIndex, z.Value));

	}

	public override void _Draw() {
		DrawBorder(this);
	}

	private void StyleButton(Button button) {
		button.SizeFlagsHorizontal = (int)Control.SizeFlags.ShrinkCenter;
		button.RectMinSize = new Vector2(btHolder.RectMinSize.x * 0.7f, 40);
		button.AddFontOverride("font", new DynamicFont() { FontData = GD.Load<DynamicFontData>("res://assets/default/Tuffy_Bold.ttf"), Size = 30 });
	}

	private void InitVignette() {
		vignette.RectMinSize = GetViewportRect().Size;
		AddChild(vignette);
		if (Lib.Node.VignetteEnabled) {
			vignette.Show();
		} else {
			vignette.Hide();
		}
	}

	public static void DrawBorder(CanvasItem canvas) {
		if (Lib.Node.BoderEnabled) {
			var vps = canvas.GetViewportRect().Size;
			int thickness = 4;
			var color = new Color(Lib.Node.BorderColorHtmlCode);
			canvas.DrawLine(new Vector2(0, 1), new Vector2(vps.x, 1), color, thickness);
			canvas.DrawLine(new Vector2(1, 0), new Vector2(1, vps.y), color, thickness);
			canvas.DrawLine(new Vector2(vps.x - 1, vps.y), new Vector2(vps.x - 1, 1), color, thickness);
			canvas.DrawLine(new Vector2(vps.x, vps.y - 1), new Vector2(1, vps.y - 1), color, thickness);
		}
	}
}
