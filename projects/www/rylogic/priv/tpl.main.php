<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" lang="en-nz">
	<head>
		<title><?=$page_title;?></title>
		<meta http-equiv="Content-Type" content="text/html; charset=UTF-8" />
		<!--
		<meta http-equiv="refresh" content="3"/>
		-->
		<link rel='stylesheet' type='text/css' href='style.css'/>
		<script type="text/javascript">
			var _gaq = _gaq || [];
			_gaq.push(['_setAccount', 'UA-11060521-1']);
			_gaq.push(['_trackPageview']);
			(function() {
				var ga = document.createElement('script');
				ga.type = 'text/javascript';
				ga.async = true;
				ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
				var s = document.getElementsByTagName('script')[0];
				s.parentNode.insertBefore(ga, s);
			})();
		</script>
	</head>
	<body>
		<?php
		require_once($rootdir.'/priv/header.php');
		echo "<div class='container'>";
			require_once($rootdir.'/priv/leftmenu.php');
			echo "<div class='content'>";
				require_once $content;
			echo "</div>";
			require_once($rootdir.'/priv/news.php');
		echo "</div>";
		require_once($rootdir.'/priv/footer.php');
		?>
	</body>
</html>

