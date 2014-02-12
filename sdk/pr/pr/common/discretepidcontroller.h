//******************************************************************************
//
//	Discrete PID controller
//
//******************************************************************************
// Usage:
//	pr::DiscretePIDController pid;
//	for( time )
//	{
//		next_control_variable = pid.Step(current_control_variable, current_output_variable, time_step);
//		UseControlVariableToChangeOutputVariable(next_control_variable);
//	}
//
#ifndef PR_DISCRETE_PID_CONTROLLER_H
#define PR_DISCRETE_PID_CONTROLLER_H

namespace pr
{
	namespace impl
	{
		template <typename T>
		class DiscretePIDController
		{
		public:
			DiscretePIDController()							{ Reset(); }

			void	Reset();
			float	GetTargetValue() const					{ return m_set_point; }
			void	SetTargetValue(float set_point)			{ m_set_point = set_point; }
			void	SetMaxAccError(float max_error)			{ m_max_acc_error = max_error; }
			void	SetProportional(float gain)				{ m_gain = gain; }
			void	SetIntegral(float time_const)			{ m_inv_time_const = 1.0f / time_const; }
			void	SetDifferential(float rate)				{ m_rate = rate; }

			float	Step(float current_input_value, float current_output_value, float time_delta_s);

		private:
			float m_set_point;
			float m_gain;
			float m_inv_time_const;
			float m_rate;
			float m_accumulative_error;
			float m_max_acc_error;
			float m_value_t_minus_1;
		};

		//**********************************************************************
		// Implementation
		//*****
		// Reset the controller
		template <typename T>
		inline void DiscretePIDController<T>::Reset()
		{
			m_set_point				= 0.0f;
			m_gain					= 0.01f;
			m_inv_time_const		= 0.0f;
			m_rate					= 0.0f;
			m_accumulative_error	= 0.0f;
			m_max_acc_error			= 1.0f;
			m_value_t_minus_1		= 0.0f;
		}

		//*****
		// Step the controller
		template <typename T>
		float DiscretePIDController<T>::Step(float current_input_value, float current_output_value, float time_delta_s)
		{
			float error_value   = m_set_point - current_output_value;

			// Proportional component
			float proportional = m_gain * error_value;

			// Integral component
			m_accumulative_error += error_value * time_delta_s;
			if( m_accumulative_error >  m_max_acc_error ) m_accumulative_error =  m_max_acc_error;
			if( m_accumulative_error < -m_max_acc_error ) m_accumulative_error = -m_max_acc_error;
			float integral		= m_inv_time_const * time_delta_s * m_accumulative_error;

			// Differential component
			float differential	= m_rate * (current_output_value - m_value_t_minus_1) / time_delta_s;
			m_value_t_minus_1 = current_output_value;

			return current_input_value + proportional + integral + differential;
		}
	}//namespace impl

	typedef impl::DiscretePIDController<void> DiscretePIDController;
}//namespace pr

#endif//PR_DISCRETE_PID_CONTROLLER_H
