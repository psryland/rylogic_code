//#define PR_VIEW3D_CREATE_STACKTRACE
using System;
using System.ComponentModel;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Maths;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		/// <summary>Reference type wrapper of a view3d light</summary>
		public class Light :INotifyPropertyChanged
		{
			public Light()
			{
				m_info = LightInfo.Ambient(0xFFFFFFFF);
			}
			public Light(LightInfo light)
			{
				m_info = light;
			}
			public Light(Colour32 ambient, Colour32 diffuse, Colour32 specular, double spec_power = 1000.0, v4? direction = null, v4? position = null)
				:this(
					direction != null ? LightInfo.Directional(direction.Value, ambient, diffuse, specular, (float)spec_power, 0) :
					position  != null ? LightInfo.Point(position.Value, ambient, diffuse, specular, (float)spec_power, 0) :
					LightInfo.Ambient(ambient))
			{}
			public Light(XElement node)
				:this()
			{
				On             = node.Element(nameof(On            )).As(On            );
				Type           = node.Element(nameof(Type          )).As(Type          );
				Position       = node.Element(nameof(Position      )).As(Position      );
				Direction      = node.Element(nameof(Direction     )).As(Direction     );
				Ambient        = node.Element(nameof(Ambient       )).As(Ambient       );
				Diffuse        = node.Element(nameof(Diffuse       )).As(Diffuse       );
				Specular       = node.Element(nameof(Specular      )).As(Specular      );
				SpecularPower  = node.Element(nameof(SpecularPower )).As(SpecularPower );
				InnerAngle     = node.Element(nameof(InnerAngle    )).As(InnerAngle    );
				OuterAngle     = node.Element(nameof(OuterAngle    )).As(OuterAngle    );
				Range          = node.Element(nameof(Range         )).As(Range         );
				Falloff        = node.Element(nameof(Falloff       )).As(Falloff       );
				CastShadow     = node.Element(nameof(CastShadow    )).As(CastShadow    );
				CameraRelative = node.Element(nameof(CameraRelative)).As(CameraRelative);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(Type          ), Type          , false);
				node.Add2(nameof(Position      ), Position      , false);
				node.Add2(nameof(Direction     ), Direction     , false);
				node.Add2(nameof(Ambient       ), Ambient       , false);
				node.Add2(nameof(Diffuse       ), Diffuse       , false);
				node.Add2(nameof(Specular      ), Specular      , false);
				node.Add2(nameof(SpecularPower ), SpecularPower , false);
				node.Add2(nameof(InnerAngle    ), InnerAngle    , false);
				node.Add2(nameof(OuterAngle    ), OuterAngle    , false);
				node.Add2(nameof(Range         ), Range         , false);
				node.Add2(nameof(Falloff       ), Falloff       , false);
				node.Add2(nameof(CastShadow    ), CastShadow    , false);
				node.Add2(nameof(On            ), On            , false);
				node.Add2(nameof(CameraRelative), CameraRelative, false);
				return node;
			}

			/// <summary>The View3d Light data</summary>
			public LightInfo Data
			{
				get => m_info;
				set => SetProp(ref m_info, value, nameof(Data));
			}
			private LightInfo m_info;

			/// <summary>Whether the light is active or not</summary>
			public bool On
			{
				get => m_info.m_on;
				set => SetProp(ref m_info.m_on, value, nameof(On));
			}

			/// <summary>The type of light source</summary>
			public ELight Type
			{
				get => m_info.m_type;
				set => SetProp(ref m_info.m_type, value, nameof(Type));
			}

			/// <summary>The position of the light source</summary>
			public v4 Position
			{
				get => m_info.m_position;
				set => SetProp(ref m_info.m_position, value, nameof(Position));
			}

			/// <summary>The direction of the light source</summary>
			public v4 Direction
			{
				get => m_info.m_direction;
				set => SetProp(ref m_info.m_direction, Math_.Normalise(value, -v4.ZAxis), nameof(Direction));
			}

			/// <summary>The colour of the ambient component of the light</summary>
			public Colour32 Ambient
			{
				get => m_info.m_ambient;
				set => SetProp(ref m_info.m_ambient, value, nameof(Ambient));
			}

			/// <summary>The colour of the diffuse component of the light</summary>
			public Colour32 Diffuse
			{
				get => m_info.m_diffuse;
				set => SetProp(ref m_info.m_diffuse, value, nameof(Diffuse));
			}

			/// <summary>The colour of the specular component of the light</summary>
			public Colour32 Specular
			{
				get => m_info.m_specular;
				set => SetProp(ref m_info.m_specular, value, nameof(Specular));
			}

			/// <summary>The specular power</summary>
			public double SpecularPower
			{
				get => m_info.m_specular_power;
				set => SetProp(ref m_info.m_specular_power, (float)value, nameof(SpecularPower));
			}

			/// <summary>The inner spot light cone angle (in degrees)</summary>
			public double InnerAngle
			{
				get => Math_.RadiansToDegrees(m_info.m_inner_angle);
				set
				{
					var angle = Math_.DegreesToRadians(Math_.Clamp(value, 0, 180));
					SetProp(ref m_info.m_inner_angle, (float)angle, nameof(InnerAngle));
				}
			}

			/// <summary>The outer spot light cone angle (in degrees)</summary>
			public double OuterAngle
			{
				get => Math_.RadiansToDegrees(m_info.m_outer_angle);
				set
				{
					var angle = Math_.DegreesToRadians(Math_.Clamp(value, 0, 180));
					SetProp(ref m_info.m_outer_angle, (float)angle, nameof(OuterAngle));
				}
			}

			/// <summary>The range of the light</summary>
			public double Range
			{
				get => m_info.m_range;
				set => SetProp(ref m_info.m_range, (float)value, nameof(Range));
			}

			/// <summary>The attenuation of the light with distance</summary>
			public double Falloff
			{
				get => m_info.m_falloff;
				set => SetProp(ref m_info.m_falloff, (float)value, nameof(Falloff));
			}

			/// <summary>The maximum distance from the light source in which objects cast shadows</summary>
			public double CastShadow
			{
				get => m_info.m_cast_shadow;
				set => SetProp(ref m_info.m_cast_shadow, (float)value, nameof(CastShadow));
			}

			/// <summary>Whether the light moves with the camera or not</summary>
			public bool CameraRelative
			{
				get => m_info.m_cam_relative;
				set => SetProp(ref m_info.m_cam_relative, value, nameof(CameraRelative));
			}

			/// <summary>Notify property value changed</summary>
			public event PropertyChangedEventHandler? PropertyChanged;
			private void SetProp<T>(ref T prop, T value, string name)
			{
				if (Equals(prop, value)) return;
				prop = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			}

			/// <summary>Implicit conversion to the value type</summary>
			public static implicit operator LightInfo(Light light) { return light.m_info; }
		}
	}
}
