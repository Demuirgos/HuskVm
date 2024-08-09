Max : [a, b] => {
	if (a > b) then {
		return a;
	} else {
		return b;
	}
}; 

Min : [a, b] => {
	if (a < b) then {
		return a;
	} else {
		return b;
	}
};

IsPrime : [n] => {
    if (n < 2) then {
        return false;
    } else {
        var i <- 2;
        while (i < n) do {
            if((n % i) = 0) then {
                return false;
            } else {
                i <- i + 1;
            }
        }
        return true;
    }
};

SumRange : [a, b] => {
    if (a = b) then {
        return 0;
    } else {
        return a + SumRange(a + 1, b);
    }
}; 